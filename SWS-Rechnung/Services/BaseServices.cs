using Microsoft.EntityFrameworkCore;
using SWSRechnung.Data;
using SWSRechnung.Models;

namespace SWSRechnung.Services
{
    public class EinstellungenService
    {
        private readonly AppDbContext _db;
        public EinstellungenService(AppDbContext db) => _db = db;

        public async Task<Dictionary<string, string>> GetAllAsync()
        {
            var list = await _db.Einstellungen.ToListAsync();
            return list.ToDictionary(e => e.Schluessel, e => e.Wert ?? "");
        }

        public async Task<string> GetAsync(string key, string fallback = "")
        {
            var e = await _db.Einstellungen.FirstOrDefaultAsync(x => x.Schluessel == key);
            return e?.Wert ?? fallback;
        }

        public async Task SaveAllAsync(Dictionary<string, string> values)
        {
            foreach (var kv in values)
            {
                var e = await _db.Einstellungen.FirstOrDefaultAsync(x => x.Schluessel == kv.Key);
                if (e == null)
                    _db.Einstellungen.Add(new Einstellung { Schluessel = kv.Key, Wert = kv.Value });
                else
                    e.Wert = kv.Value;
            }
            await _db.SaveChangesAsync();
        }
    }

    public class NummernService
    {
        private readonly AppDbContext _db;
        private readonly EinstellungenService _einst;
        public NummernService(AppDbContext db, EinstellungenService einst) { _db = db; _einst = einst; }

        public async Task<string> NaechsteAngebotsnummerAsync()
        {
            var prefix = await _einst.GetAsync("AngebotPrefix", "ANG");
            var nr     = int.Parse(await _einst.GetAsync("AngebotNaechsteNr", "1"));
            var year   = DateTime.Today.Year;
            await _einst.SaveAllAsync(new() { ["AngebotNaechsteNr"] = (nr + 1).ToString() });
            return $"{prefix}-{year}-{nr:D4}";
        }

        public async Task<string> NaechsteRechnungsnummerAsync()
        {
            var prefix = await _einst.GetAsync("RechnungPrefix", "RE");
            var nr     = int.Parse(await _einst.GetAsync("RechnungNaechsteNr", "1"));
            var year   = DateTime.Today.Year;
            await _einst.SaveAllAsync(new() { ["RechnungNaechsteNr"] = (nr + 1).ToString() });
            return $"{prefix}-{year}-{nr:D4}";
        }
    }
}
