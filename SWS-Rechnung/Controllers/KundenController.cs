using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWSRechnung.Data;
using SWSRechnung.Models;

namespace SWSRechnung.Controllers
{
    public class KundenController : Controller
    {
        private readonly AppDbContext _db;
        public KundenController(AppDbContext db) => _db = db;

        public async Task<IActionResult> Index(string? q, bool? nurAktiv)
        {
            var query = _db.Kunden.AsQueryable();
            if (!string.IsNullOrEmpty(q))
                query = query.Where(k => k.Firmenname.Contains(q)
                    || (k.Ansprechpartner != null && k.Ansprechpartner.Contains(q))
                    || (k.Email != null && k.Email.Contains(q))
                    || (k.Ort != null && k.Ort.Contains(q)));
            if (nurAktiv == true) query = query.Where(k => k.Aktiv);
            ViewBag.Q = q; ViewBag.NurAktiv = nurAktiv;
            return View(await query.OrderBy(k => k.Firmenname).ToListAsync());
        }

        public async Task<IActionResult> Details(int id)
        {
            var k = await _db.Kunden
                .Include(x => x.Angebote)
                .Include(x => x.Rechnungen).ThenInclude(r => r.Positionen)
                .FirstOrDefaultAsync(x => x.Id == id);
            if (k == null) return NotFound();
            return View(k);
        }

        public IActionResult Neu() => View(new Kunde());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Neu(Kunde k)
        {
            if (!ModelState.IsValid) return View(k);
            k.ErstelltAm = DateTime.Now;
            // Auto-Kundennummer
            var letzte = await _db.Kunden
                .Where(x => x.Kundennummer != null && x.Kundennummer.StartsWith("KD-"))
                .OrderByDescending(x => x.Id).Select(x => x.Kundennummer).FirstOrDefaultAsync();
            int nr = 1;
            if (letzte != null && int.TryParse(letzte[3..], out int parsed)) nr = parsed + 1;
            k.Kundennummer = $"KD-{nr:D4}";
            _db.Kunden.Add(k);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Kunde \"{k.Firmenname}\" wurde angelegt (Nr. {k.Kundennummer}).";
            return RedirectToAction(nameof(Details), new { id = k.Id });
        }

        public async Task<IActionResult> Bearbeiten(int id)
        {
            var k = await _db.Kunden.FindAsync(id);
            if (k == null) return NotFound();
            return View(k);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Bearbeiten(int id, Kunde k)
        {
            if (id != k.Id) return NotFound();
            if (!ModelState.IsValid) return View(k);
            try { _db.Update(k); await _db.SaveChangesAsync(); }
            catch (DbUpdateConcurrencyException) { if (!_db.Kunden.Any(x => x.Id == id)) return NotFound(); throw; }
            TempData["Success"] = "Kundendaten aktualisiert.";
            return RedirectToAction(nameof(Details), new { id = k.Id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Loeschen(int id)
        {
            var k = await _db.Kunden.FindAsync(id);
            if (k == null) return RedirectToAction(nameof(Index));
            bool hatDoks = await _db.Angebote.AnyAsync(a => a.KundeId == id)
                        || await _db.Rechnungen.AnyAsync(r => r.KundeId == id);
            if (hatDoks)
            { TempData["Error"] = "Kunde hat Angebote/Rechnungen und kann nicht gelöscht werden."; return RedirectToAction(nameof(Details), new { id }); }
            _db.Kunden.Remove(k);
            await _db.SaveChangesAsync();
            TempData["Success"] = $"Kunde \"{k.Firmenname}\" gelöscht.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> StatusToggle(int id)
        {
            var k = await _db.Kunden.FindAsync(id);
            if (k != null) { k.Aktiv = !k.Aktiv; await _db.SaveChangesAsync(); }
            return RedirectToAction(nameof(Details), new { id });
        }
    }
}
