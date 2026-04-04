using Microsoft.EntityFrameworkCore;
using SWSRechnung.Models;

namespace SWSRechnung.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Kunde>            Kunden             { get; set; }
        public DbSet<Angebot>          Angebote           { get; set; }
        public DbSet<AngebotPosition>  AngebotPositionen  { get; set; }
        public DbSet<Rechnung>         Rechnungen         { get; set; }
        public DbSet<RechnungPosition> RechnungPositionen { get; set; }
        public DbSet<Einstellung>      Einstellungen      { get; set; }

        protected override void OnModelCreating(ModelBuilder b)
        {
            base.OnModelCreating(b);

            // ── Einstellung: vollständig explizit konfigurieren ─────
            // Ohne dies verwechselt EF Core 8 den Typ mit dem internen
            // Dictionary<string,object>-Shared-Entity-Mechanismus.
            b.Entity<Einstellung>(e =>
            {
                e.ToTable("Einstellungen");
                e.HasKey(x => x.Id);
                e.Property(x => x.Id).ValueGeneratedOnAdd();
                e.Property(x => x.Schluessel).IsRequired().HasMaxLength(100);
                e.Property(x => x.Wert).IsRequired(false);
            });

            // ── Kunde ───────────────────────────────────────────────
            b.Entity<Kunde>(e =>
            {
                e.HasMany(k => k.Angebote).WithOne(a => a.Kunde)
                 .HasForeignKey(a => a.KundeId).OnDelete(DeleteBehavior.Restrict);
                e.HasMany(k => k.Rechnungen).WithOne(r => r.Kunde)
                 .HasForeignKey(r => r.KundeId).OnDelete(DeleteBehavior.Restrict);
            });

            // ── Angebot ─────────────────────────────────────────────
            b.Entity<Angebot>(e =>
            {
                e.HasMany(a => a.Positionen).WithOne(p => p.Angebot)
                 .HasForeignKey(p => p.AngebotId).OnDelete(DeleteBehavior.Cascade);
                e.Property(a => a.MwStSatz).HasPrecision(5, 2);
            });

            b.Entity<AngebotPosition>(e =>
            {
                e.Property(p => p.Menge).HasPrecision(10, 3);
                e.Property(p => p.Einzelpreis).HasPrecision(12, 4);
                e.Property(p => p.Rabatt).HasPrecision(5, 2);
            });

            // ── Rechnung ────────────────────────────────────────────
            b.Entity<Rechnung>(e =>
            {
                e.HasMany(r => r.Positionen).WithOne(p => p.Rechnung)
                 .HasForeignKey(p => p.RechnungId).OnDelete(DeleteBehavior.Cascade);
                e.Property(r => r.MwStSatz).HasPrecision(5, 2);
            });

            b.Entity<RechnungPosition>(e =>
            {
                e.Property(p => p.Menge).HasPrecision(10, 3);
                e.Property(p => p.Einzelpreis).HasPrecision(12, 4);
                e.Property(p => p.Rabatt).HasPrecision(5, 2);
            });

            // ── Seed: Firmen-Einstellungen ──────────────────────────
            b.Entity<Einstellung>().HasData(
                new Einstellung { Id=1,  Schluessel="FirmaName",            Wert="steinwald.soft GmbH" },
                new Einstellung { Id=2,  Schluessel="FirmaStrasse",          Wert="Sch\u00f6nfu\u00dfstr. 41" },
                new Einstellung { Id=3,  Schluessel="FirmaPLZ",              Wert="95688" },
                new Einstellung { Id=4,  Schluessel="FirmaOrt",              Wert="Friedenfels" },
                new Einstellung { Id=5,  Schluessel="FirmaLand",             Wert="Deutschland" },
                new Einstellung { Id=6,  Schluessel="FirmaTelefon",          Wert="+49 (0) 1765 999 7878" },
                new Einstellung { Id=7,  Schluessel="FirmaEmail",            Wert="info@steinwaldsoft.de" },
                new Einstellung { Id=8,  Schluessel="FirmaWebsite",          Wert="www.steinwaldsoft.de" },
                new Einstellung { Id=9,  Schluessel="FirmaUstId",            Wert="DE354987741" },
                new Einstellung { Id=10, Schluessel="FirmaRegistergericht",  Wert="Registergericht Weiden i. d. Opf." },
                new Einstellung { Id=11, Schluessel="FirmaHandelsregister",  Wert="HRB 5891" },
                new Einstellung { Id=12, Schluessel="BankName",              Wert="Raiffeisenbank Oberpfalz NordWest eG" },
                new Einstellung { Id=13, Schluessel="BankIBAN",              Wert="DE51 7706 9764 0006 4667 37" },
                new Einstellung { Id=14, Schluessel="BankBIC",               Wert="GENODEF1KEM" },
                new Einstellung { Id=15, Schluessel="AngebotPrefix",         Wert="ANG" },
                new Einstellung { Id=16, Schluessel="RechnungPrefix",        Wert="RE" },
                new Einstellung { Id=17, Schluessel="AngebotNaechsteNr",     Wert="1" },
                new Einstellung { Id=18, Schluessel="RechnungNaechsteNr",    Wert="1" },
                new Einstellung { Id=19, Schluessel="Zahlungsziel",          Wert="14" },
                new Einstellung { Id=20, Schluessel="AngebotEinleitung",
                    Wert="vielen Dank f\u00fcr Ihre Anfrage. Gerne unterbreiten wir Ihnen folgendes Angebot:" },
                new Einstellung { Id=21, Schluessel="AngebotSchlusstext",
                    Wert="Wir freuen uns auf Ihre R\u00fcckmeldung.\n\nMit freundlichen Gr\u00fc\u00dfen\nsteinwald.soft GmbH" },
                new Einstellung { Id=22, Schluessel="RechnungEinleitung",
                    Wert="wir erlauben uns, Ihnen folgende Leistungen in Rechnung zu stellen:" },
                new Einstellung { Id=23, Schluessel="RechnungSchlusstext",
                    Wert="Gem\u00e4\u00df \u00a719 UStG enth\u00e4lt der Rechnungsbetrag keine Umsatzsteuer." +
                    "\nBitte \u00fcberweisen Sie den Rechnungsbetrag mit Angabe der Rechnungsnummer innerhalb von 14 Tagen " +
                    "auf das unten genannte Konto.\n\nVielen Dank.\n\nsteinwald.soft GmbH" }
            );
        }
    }
}
