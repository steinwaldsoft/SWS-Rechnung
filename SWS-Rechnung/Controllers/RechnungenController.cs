using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SWSRechnung.Data;
using SWSRechnung.Models;
using SWSRechnung.Services;

namespace SWSRechnung.Controllers
{
    public class RechnungenController : Controller
    {
        private readonly AppDbContext _db;
        private readonly NummernService _nr;
        private readonly PdfService _pdf;
        private readonly EinstellungenService _einst;
        private readonly ZugferdService _zugferd;
        private readonly DocxService _docx;

        public RechnungenController(AppDbContext db, NummernService nr, PdfService pdf,
            EinstellungenService einst, ZugferdService zugferd, DocxService docx)
        { _db = db; _nr = nr; _pdf = pdf; _einst = einst; _zugferd = zugferd; _docx = docx; }

        // ── Index ─────────────────────────────────────────────────
        public async Task<IActionResult> Index(RechnungStatus? status, int? kundeId, string? q, int? jahr)
        {
            // Auto-mark überfällige
            var heute = DateTime.Today;
            var ueberfaellig = await _db.Rechnungen
                .Where(r => r.Status == RechnungStatus.Versendet && r.FaelligAm < heute).ToListAsync();
            if (ueberfaellig.Any())
            {
                ueberfaellig.ForEach(r => r.Status = RechnungStatus.Ueberfaellig);
                await _db.SaveChangesAsync();
            }

            var query = _db.Rechnungen.Include(r=>r.Kunde).Include(r=>r.Positionen).AsQueryable();
            if (status.HasValue)  query = query.Where(r => r.Status == status.Value);
            if (kundeId.HasValue) query = query.Where(r => r.KundeId == kundeId.Value);
            if (jahr.HasValue)    query = query.Where(r => r.Rechnungsdatum.Year == jahr.Value);
            if (!string.IsNullOrEmpty(q))
                query = query.Where(r => r.Rechnungsnummer.Contains(q) || (r.Betreff!=null && r.Betreff.Contains(q)) || r.Kunde!.Firmenname.Contains(q));

            ViewBag.StatusFilter = status; ViewBag.KundeFilter = kundeId; ViewBag.JahrFilter = jahr; ViewBag.Q = q;
            ViewBag.Kunden = new SelectList(await _db.Kunden.Where(k=>k.Aktiv).OrderBy(k=>k.Firmenname).ToListAsync(),"Id","Firmenname");
            ViewBag.Jahre  = await _db.Rechnungen.Select(r=>r.Rechnungsdatum.Year).Distinct().OrderByDescending(y=>y).ToListAsync();
            return View(await query.OrderByDescending(r=>r.Rechnungsdatum).ToListAsync());
        }

        // ── Details ───────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var r = await _db.Rechnungen.Include(x=>x.Kunde).Include(x=>x.Angebot).Include(x=>x.Positionen)
                             .FirstOrDefaultAsync(x=>x.Id==id);
            if (r == null) return NotFound();
            return View(r);
        }

        // ── Neu ───────────────────────────────────────────────────
        public async Task<IActionResult> Neu(int? kundeId)
        {
            await SetKundenSelectAsync(kundeId);
            int ziel = int.TryParse(await _einst.GetAsync("Zahlungsziel","14"), out int z) ? z : 14;
            return View(new Rechnung {
                Rechnungsnummer = await _nr.NaechsteRechnungsnummerAsync(),
                Rechnungsdatum  = DateTime.Today,
                FaelligAm       = DateTime.Today.AddDays(ziel),
                MwStSatz        = 0m,
                KundeId         = kundeId ?? 0,
                Einleitung      = await _einst.GetAsync("RechnungEinleitung"),
                Schlusstext     = await _einst.GetAsync("RechnungSchlusstext"),
                Positionen      = new List<RechnungPosition>{ new() { Position=1, Einheit="Std." } }
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Neu(Rechnung r)
        {
            r.Positionen = r.Positionen.Where(p=>!string.IsNullOrEmpty(p.Bezeichnung)).OrderBy(p=>p.Position).ToList();
            if (!ModelState.IsValid) { await SetKundenSelectAsync(r.KundeId); return View(r); }
            r.ErstelltAm = DateTime.Now;
            _db.Rechnungen.Add(r);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Rechnung {r.Rechnungsnummer} erstellt.";
            return RedirectToAction(nameof(Details), new { id=r.Id });
        }

        // ── Bearbeiten ────────────────────────────────────────────
        public async Task<IActionResult> Bearbeiten(int id)
        {
            var r = await _db.Rechnungen.Include(x=>x.Positionen).FirstOrDefaultAsync(x=>x.Id==id);
            if (r == null) return NotFound();
            await SetKundenSelectAsync(r.KundeId);
            r.Positionen = r.Positionen.OrderBy(p=>p.Position).ToList();
            return View(r);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Bearbeiten(int id, Rechnung r)
        {
            if (id != r.Id) return NotFound();
            r.Positionen = r.Positionen.Where(p=>!string.IsNullOrEmpty(p.Bezeichnung)).OrderBy(p=>p.Position).ToList();
            if (!ModelState.IsValid) { await SetKundenSelectAsync(r.KundeId); return View(r); }
            var exist = await _db.Rechnungen.Include(x=>x.Positionen).FirstOrDefaultAsync(x=>x.Id==id);
            if (exist == null) return NotFound();
            _db.RechnungPositionen.RemoveRange(exist.Positionen);
            exist.KundeId=r.KundeId; exist.Betreff=r.Betreff; exist.Rechnungsdatum=r.Rechnungsdatum;
            exist.LeistungVon=r.LeistungVon; exist.LeistungBis=r.LeistungBis; exist.FaelligAm=r.FaelligAm;
            exist.Status=r.Status; exist.Einleitung=r.Einleitung; exist.Schlusstext=r.Schlusstext;
            exist.Notizen=r.Notizen; exist.MwStSatz=r.MwStSatz; exist.BezahltAm=r.BezahltAm;
            exist.Positionen=r.Positionen;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Rechnung aktualisiert.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── Als bezahlt markieren ─────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AlsBezahlt(int id)
        {
            var r = await _db.Rechnungen.FindAsync(id);
            if (r != null) { r.Status=RechnungStatus.Bezahlt; r.BezahltAm=DateTime.Today; await _db.SaveChangesAsync(); TempData["Success"]="Als bezahlt markiert."; }
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StatusAendern(int id, int status)
        {
            var r = await _db.Rechnungen.FindAsync(id);
            if (r != null) {
                r.Status = (RechnungStatus)status;
                if (r.Status == RechnungStatus.Bezahlt && !r.BezahltAm.HasValue) r.BezahltAm = DateTime.Today;
                await _db.SaveChangesAsync();
                TempData["Success"] = "Status geändert.";
            }
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── Stornieren ────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Stornieren(int id)
        {
            var r = await _db.Rechnungen.FindAsync(id);
            if (r != null) { r.Status=RechnungStatus.Storniert; await _db.SaveChangesAsync(); TempData["Success"]="Rechnung storniert."; }
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── Löschen ───────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Loeschen(int id)
        {
            var r = await _db.Rechnungen.FindAsync(id);
            if (r != null) { _db.Rechnungen.Remove(r); await _db.SaveChangesAsync(); TempData["Success"]="Rechnung gelöscht."; }
            return RedirectToAction(nameof(Index));
        }

        // ── Word (.docx) ──────────────────────────────────────────
        public async Task<IActionResult> Word(int id)
        {
            var r = await _db.Rechnungen.Include(x=>x.Kunde).Include(x=>x.Positionen).FirstOrDefaultAsync(x=>x.Id==id);
            if (r == null) return NotFound();
            var bytes = await _docx.RechnungDocxAsync(r);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"Rechnung_{r.Rechnungsnummer}.docx");
        }

        // ── XRechnung XML ─────────────────────────────────────────
        public async Task<IActionResult> XRechnung(int id)
        {
            var r = await _db.Rechnungen.Include(x=>x.Kunde).Include(x=>x.Positionen).FirstOrDefaultAsync(x=>x.Id==id);
            if (r == null) return NotFound();
            var bytes = await _zugferd.XRechnungXmlAsync(r);
            return File(bytes, "application/xml", $"XRechnung_{r.Rechnungsnummer}.xml");
        }

        // ── ZUGFeRD PDF (PDF + eingebettetes XML) ─────────────────
        public async Task<IActionResult> Zugferd(int id)
        {
            var r = await _db.Rechnungen.Include(x=>x.Kunde).Include(x=>x.Positionen).FirstOrDefaultAsync(x=>x.Id==id);
            if (r == null) return NotFound();
            var xml   = await _zugferd.ZugferdXmlStringAsync(r);
            var bytes = await _pdf.RechnungZugferdAsync(r, xml);
            return File(bytes, "application/pdf", $"ZUGFeRD_{r.Rechnungsnummer}.pdf");
        }

        private async Task SetKundenSelectAsync(int? selected)
        {
            var ks = await _db.Kunden.Where(k=>k.Aktiv).OrderBy(k=>k.Firmenname).ToListAsync();
            ViewBag.Kunden = new SelectList(ks,"Id","Firmenname",selected);
        }
    }
}
