using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SWSRechnung.Data;
using SWSRechnung.Models;
using SWSRechnung.Services;

namespace SWSRechnung.Controllers
{
    public class HomeController : Controller
    {
        private readonly AppDbContext _db;
        private readonly EinstellungenService _einst;
        public HomeController(AppDbContext db, EinstellungenService einst) { _db = db; _einst = einst; }

        public async Task<IActionResult> Index()
        {
            var heute = DateTime.Today;
            var monatsStart = new DateTime(heute.Year, heute.Month, 1);

            var vm = new DashboardVm
            {
                AnzahlKunden = await _db.Kunden.CountAsync(k => k.Aktiv),
                OffeneAngebote = await _db.Angebote
                    .CountAsync(a => a.Status == AngebotStatus.Entwurf || a.Status == AngebotStatus.Versendet),
                OffeneRechnungen = await _db.Rechnungen
                    .CountAsync(r => r.Status == RechnungStatus.Versendet),
                UeberfaelligeRechnungen = await _db.Rechnungen
                    .CountAsync(r => r.Status == RechnungStatus.Versendet && r.FaelligAm < heute),
                UmsatzMonat = await _db.Rechnungen
                    .Where(r => r.Status == RechnungStatus.Bezahlt && r.BezahltAm >= monatsStart)
                    .Include(r => r.Positionen)
                    .ToListAsync() is var rm ? rm.Sum(r => r.Bruttobetrag) : 0,
                UmsatzGesamt = await _db.Rechnungen
                    .Where(r => r.Status == RechnungStatus.Bezahlt)
                    .Include(r => r.Positionen)
                    .ToListAsync() is var rg ? rg.Sum(r => r.Bruttobetrag) : 0,
                LetzteAngebote = await _db.Angebote.Include(a => a.Kunde)
                    .OrderByDescending(a => a.ErstelltAm).Take(6).ToListAsync(),
                LetzteRechnungen = await _db.Rechnungen.Include(r => r.Kunde).Include(r => r.Positionen)
                    .OrderByDescending(r => r.ErstelltAm).Take(6).ToListAsync(),
                TopUeberfaellige = await _db.Rechnungen
                    .Include(r => r.Kunde).Include(r => r.Positionen)
                    .Where(r => r.Status == RechnungStatus.Versendet && r.FaelligAm < heute)
                    .OrderBy(r => r.FaelligAm).Take(5).ToListAsync()
            };
            return View(vm);
        }

        public async Task<IActionResult> Einstellungen()
        {
            ViewData["Title"] = "Einstellungen";
            var d = await _einst.GetAllAsync();
            return View(d);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Einstellungen(IFormCollection form)
        {
            var values = new Dictionary<string, string>();

            // Formularfelder haben das Muster:  einstellungen[Schluessel]
            foreach (var key in form.Keys)
            {
                if (key.StartsWith("einstellungen[") && key.EndsWith("]"))
                {
                    var schluessel = key[14..^1]; // alles zwischen [ und ]
                    values[schluessel] = form[key].ToString();
                }
            }

            if (values.Count > 0)
                await _einst.SaveAllAsync(values);

            TempData["Success"] = "Einstellungen gespeichert.";
            return RedirectToAction(nameof(Einstellungen));
        }

        [ResponseCache(Duration=0, Location=ResponseCacheLocation.None, NoStore=true)]
        public IActionResult Error() => View();
    }

    public class DashboardVm
    {
        public int     AnzahlKunden            { get; set; }
        public int     OffeneAngebote          { get; set; }
        public int     OffeneRechnungen        { get; set; }
        public int     UeberfaelligeRechnungen { get; set; }
        public decimal UmsatzMonat             { get; set; }
        public decimal UmsatzGesamt            { get; set; }
        public List<Angebot>  LetzteAngebote    { get; set; } = new();
        public List<Rechnung> LetzteRechnungen  { get; set; } = new();
        public List<Rechnung> TopUeberfaellige  { get; set; } = new();
    }
}
