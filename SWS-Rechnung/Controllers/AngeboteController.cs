using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SWSRechnung.Data;
using SWSRechnung.Models;
using SWSRechnung.Services;

namespace SWSRechnung.Controllers
{
    public class AngeboteController : Controller
    {
        private readonly AppDbContext _db;
        private readonly NummernService _nr;
        private readonly PdfService _pdf;
        private readonly EinstellungenService _einst;
        private readonly DocxService _docx;

        public AngeboteController(AppDbContext db, NummernService nr, PdfService pdf,
            EinstellungenService einst, DocxService docx)
        { _db = db; _nr = nr; _pdf = pdf; _einst = einst; _docx = docx; }

        // ── Index ─────────────────────────────────────────────────
        public async Task<IActionResult> Index(AngebotStatus? status, int? kundeId, string? q)
        {
            var query = _db.Angebote.Include(a => a.Kunde).Include(a => a.Positionen).AsQueryable();
            if (status.HasValue)  query = query.Where(a => a.Status == status.Value);
            if (kundeId.HasValue) query = query.Where(a => a.KundeId == kundeId.Value);
            if (!string.IsNullOrEmpty(q))
                query = query.Where(a => a.Angebotsnummer.Contains(q) || (a.Betreff != null && a.Betreff.Contains(q)) || a.Kunde!.Firmenname.Contains(q));
            ViewBag.StatusFilter = status; ViewBag.KundeFilter = kundeId; ViewBag.Q = q;
            ViewBag.Kunden = new SelectList(await _db.Kunden.Where(k=>k.Aktiv).OrderBy(k=>k.Firmenname).ToListAsync(), "Id","Firmenname");
            return View(await query.OrderByDescending(a => a.Angebotsdatum).ToListAsync());
        }

        // ── Details ───────────────────────────────────────────────
        public async Task<IActionResult> Details(int id)
        {
            var a = await _db.Angebote.Include(x=>x.Kunde).Include(x=>x.Positionen)
                             .FirstOrDefaultAsync(x=>x.Id==id);
            if (a == null) return NotFound();
            return View(a);
        }

        // ── Neu ───────────────────────────────────────────────────
        public async Task<IActionResult> Neu(int? kundeId)
        {
            await SetKundenSelectAsync(kundeId);
            decimal mwst = decimal.TryParse(await _einst.GetAsync("MwStSatz","0"),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out var m) ? m : 0m;
            int gueltig = int.TryParse(await _einst.GetAsync("AngebotGueltigkeitTage","30"), out var g) ? g : 30;
            return View(new Angebot {
                Angebotsnummer = await _nr.NaechsteAngebotsnummerAsync(),
                Angebotsdatum  = DateTime.Today,
                GueltigBis     = DateTime.Today.AddDays(gueltig),
                MwStSatz       = mwst,
                KundeId        = kundeId ?? 0,
                Einleitung     = await _einst.GetAsync("AngebotEinleitung"),
                Schlusstext    = await _einst.GetAsync("AngebotSchlusstext"),
                Positionen     = new List<AngebotPosition>{ new() { Position=1, Einheit="Std." } }
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Neu(Angebot a)
        {
            a.Positionen = a.Positionen.Where(p => !string.IsNullOrEmpty(p.Bezeichnung))
                           .OrderBy(p=>p.Position).ToList();
            if (!ModelState.IsValid) { await SetKundenSelectAsync(a.KundeId); return View(a); }
            a.ErstelltAm = DateTime.Now;
            _db.Angebote.Add(a);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Angebot {a.Angebotsnummer} erstellt.";
            return RedirectToAction(nameof(Details), new { id=a.Id });
        }

        // ── Bearbeiten ────────────────────────────────────────────
        public async Task<IActionResult> Bearbeiten(int id)
        {
            var a = await _db.Angebote.Include(x=>x.Positionen).FirstOrDefaultAsync(x=>x.Id==id);
            if (a == null) return NotFound();
            await SetKundenSelectAsync(a.KundeId);
            a.Positionen = a.Positionen.OrderBy(p=>p.Position).ToList();
            return View(a);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Bearbeiten(int id, Angebot a)
        {
            if (id != a.Id) return NotFound();
            a.Positionen = a.Positionen.Where(p=>!string.IsNullOrEmpty(p.Bezeichnung)).OrderBy(p=>p.Position).ToList();
            if (!ModelState.IsValid) { await SetKundenSelectAsync(a.KundeId); return View(a); }
            var exist = await _db.Angebote.Include(x=>x.Positionen).FirstOrDefaultAsync(x=>x.Id==id);
            if (exist == null) return NotFound();
            _db.AngebotPositionen.RemoveRange(exist.Positionen);
            exist.KundeId=a.KundeId; exist.Betreff=a.Betreff; exist.Angebotsdatum=a.Angebotsdatum;
            exist.GueltigBis=a.GueltigBis; exist.Status=a.Status; exist.Einleitung=a.Einleitung;
            exist.Schlusstext=a.Schlusstext; exist.Notizen=a.Notizen; exist.MwStSatz=a.MwStSatz;
            exist.Positionen=a.Positionen;
            await _db.SaveChangesAsync();
            TempData["Success"] = "Angebot aktualisiert.";
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── Status ändern ─────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StatusAendern(int id, int status)
        {
            var a = await _db.Angebote.FindAsync(id);
            if (a != null) { a.Status = (AngebotStatus)status; await _db.SaveChangesAsync(); TempData["Success"] = "Status geändert."; }
            return RedirectToAction(nameof(Details), new { id });
        }

        // ── Als Rechnung übernehmen ───────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AlsRechnung(int id)
        {
            var a = await _db.Angebote.Include(x=>x.Positionen).FirstOrDefaultAsync(x=>x.Id==id);
            if (a == null) return NotFound();
            var r = new Rechnung {
                KundeId       = a.KundeId,
                AngebotId     = a.Id,
                Betreff       = a.Betreff,
                Rechnungsdatum= DateTime.Today,
                FaelligAm     = DateTime.Today.AddDays(14),
                MwStSatz      = a.MwStSatz,
                Einleitung    = await _einst.GetAsync("RechnungEinleitung"),
                Schlusstext   = await _einst.GetAsync("RechnungSchlusstext"),
                Positionen    = a.Positionen.Select(p => new RechnungPosition {
                    Position=p.Position, Bezeichnung=p.Bezeichnung, Beschreibung=p.Beschreibung,
                    Menge=p.Menge, Einheit=p.Einheit, Einzelpreis=p.Einzelpreis, Rabatt=p.Rabatt
                }).ToList()
            };
            r.Rechnungsnummer = await _nr.NaechsteRechnungsnummerAsync();
            r.ErstelltAm = DateTime.Now;
            _db.Rechnungen.Add(r);
            a.Status = AngebotStatus.Angenommen;
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Rechnung {r.Rechnungsnummer} aus Angebot {a.Angebotsnummer} erstellt.";
            return RedirectToAction("Details", "Rechnungen", new { id=r.Id });
        }

        // ── Löschen ───────────────────────────────────────────────
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Loeschen(int id)
        {
            var a = await _db.Angebote.FindAsync(id);
            if (a != null) { _db.Angebote.Remove(a); await _db.SaveChangesAsync(); TempData["Success"] = "Angebot gelöscht."; }
            return RedirectToAction(nameof(Index));
        }

        // ── Word (.docx) ──────────────────────────────────────────
        public async Task<IActionResult> Word(int id)
        {
            var a = await _db.Angebote.Include(x=>x.Kunde).Include(x=>x.Positionen).FirstOrDefaultAsync(x=>x.Id==id);
            if (a == null) return NotFound();
            var bytes = await _docx.AngebotDocxAsync(a);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                $"Angebot_{a.Angebotsnummer}.docx");
        }

        private async Task SetKundenSelectAsync(int? selected)
        {
            var ks = await _db.Kunden.Where(k=>k.Aktiv).OrderBy(k=>k.Firmenname).ToListAsync();
            ViewBag.Kunden = new SelectList(ks, "Id","Firmenname", selected);
        }
    }
}
