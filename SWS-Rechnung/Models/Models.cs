using System.ComponentModel.DataAnnotations;

namespace SWSRechnung.Models
{
    // ── Kunde ─────────────────────────────────────────────────────
    public class Kunde
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Firmenname ist erforderlich")]
        [Display(Name = "Firmenname")]
        [StringLength(200)]
        public string Firmenname { get; set; } = string.Empty;

        [Display(Name = "Ansprechpartner")]
        [StringLength(200)]
        public string? Ansprechpartner { get; set; }

        [Display(Name = "Anrede")]
        public string? Anrede { get; set; }

        [Display(Name = "Straße / Nr.")]
        [StringLength(200)]
        public string? Strasse { get; set; }

        [Display(Name = "PLZ")]
        [StringLength(10)]
        public string? PLZ { get; set; }

        [Display(Name = "Ort")]
        [StringLength(100)]
        public string? Ort { get; set; }

        [Display(Name = "Land")]
        [StringLength(100)]
        public string Land { get; set; } = "Deutschland";

        [Display(Name = "Telefon")]
        [StringLength(50)]
        public string? Telefon { get; set; }

        [Display(Name = "E-Mail")]
        [EmailAddress(ErrorMessage = "Ungültige E-Mail-Adresse")]
        [StringLength(200)]
        public string? Email { get; set; }

        [Display(Name = "Website")]
        [StringLength(200)]
        public string? Website { get; set; }

        [Display(Name = "USt-IdNr.")]
        [StringLength(50)]
        public string? UstIdNr { get; set; }

        [Display(Name = "Kundennummer")]
        [StringLength(20)]
        public string? Kundennummer { get; set; }

        [Display(Name = "Notizen")]
        public string? Notizen { get; set; }

        [Display(Name = "Erstellt am")]
        public DateTime ErstelltAm { get; set; } = DateTime.Now;

        [Display(Name = "Aktiv")]
        public bool Aktiv { get; set; } = true;

        public ICollection<Angebot> Angebote { get; set; } = new List<Angebot>();
        public ICollection<Rechnung> Rechnungen { get; set; } = new List<Rechnung>();

        public string DisplayName => string.IsNullOrEmpty(Ansprechpartner)
            ? Firmenname : $"{Firmenname} ({Ansprechpartner})";
    }

    // ── Angebot ───────────────────────────────────────────────────
    public class Angebot
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Angebotsnummer")]
        [StringLength(30)]
        public string Angebotsnummer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte Kunden auswählen")]
        [Display(Name = "Kunde")]
        public int KundeId { get; set; }
        public Kunde? Kunde { get; set; }

        [Display(Name = "Betreff")]
        [StringLength(300)]
        public string? Betreff { get; set; }

        [Display(Name = "Angebotsdatum")]
        [DataType(DataType.Date)]
        public DateTime Angebotsdatum { get; set; } = DateTime.Today;

        [Display(Name = "Gültig bis")]
        [DataType(DataType.Date)]
        public DateTime? GueltigBis { get; set; }

        [Display(Name = "Status")]
        public AngebotStatus Status { get; set; } = AngebotStatus.Entwurf;

        [Display(Name = "Einleitung")]
        public string? Einleitung { get; set; }

        [Display(Name = "Schlusstext")]
        public string? Schlusstext { get; set; }

        [Display(Name = "Interne Notizen")]
        public string? Notizen { get; set; }

        [Display(Name = "MwSt. (%)")]
        public decimal MwStSatz { get; set; } = 19m;

        [Display(Name = "Erstellt am")]
        public DateTime ErstelltAm { get; set; } = DateTime.Now;

        public ICollection<AngebotPosition> Positionen { get; set; } = new List<AngebotPosition>();

        public decimal Nettobetrag => Positionen.Sum(p => p.Gesamtpreis);
        public decimal MwStBetrag  => Nettobetrag * (MwStSatz / 100m);
        public decimal Bruttobetrag => Nettobetrag + MwStBetrag;
    }

    public class AngebotPosition
    {
        public int Id { get; set; }
        public int AngebotId { get; set; }
        public Angebot? Angebot { get; set; }

        [Display(Name = "Pos.")]
        public int Position { get; set; }

        [Required]
        [Display(Name = "Bezeichnung")]
        [StringLength(500)]
        public string Bezeichnung { get; set; } = string.Empty;

        [Display(Name = "Beschreibung")]
        public string? Beschreibung { get; set; }

        [Display(Name = "Menge")]
        public decimal Menge { get; set; } = 1m;

        [Display(Name = "Einheit")]
        [StringLength(20)]
        public string Einheit { get; set; } = "Std.";

        [Display(Name = "Einzelpreis (€)")]
        public decimal Einzelpreis { get; set; }

        [Display(Name = "Rabatt (%)")]
        public decimal Rabatt { get; set; }

        public decimal Gesamtpreis => Menge * Einzelpreis * (1m - Rabatt / 100m);
    }

    // ── Rechnung ──────────────────────────────────────────────────
    public class Rechnung
    {
        public int Id { get; set; }

        [Required]
        [Display(Name = "Rechnungsnummer")]
        [StringLength(30)]
        public string Rechnungsnummer { get; set; } = string.Empty;

        [Required(ErrorMessage = "Bitte Kunden auswählen")]
        [Display(Name = "Kunde")]
        public int KundeId { get; set; }
        public Kunde? Kunde { get; set; }

        [Display(Name = "Referenz Angebot")]
        public int? AngebotId { get; set; }
        public Angebot? Angebot { get; set; }

        [Display(Name = "Betreff")]
        [StringLength(300)]
        public string? Betreff { get; set; }

        [Display(Name = "Rechnungsdatum")]
        [DataType(DataType.Date)]
        public DateTime Rechnungsdatum { get; set; } = DateTime.Today;

        [Display(Name = "Leistung von")]
        [DataType(DataType.Date)]
        public DateTime? LeistungVon { get; set; }

        [Display(Name = "Leistung bis")]
        [DataType(DataType.Date)]
        public DateTime? LeistungBis { get; set; }

        [Display(Name = "Fällig am")]
        [DataType(DataType.Date)]
        public DateTime? FaelligAm { get; set; }

        [Display(Name = "Status")]
        public RechnungStatus Status { get; set; } = RechnungStatus.Entwurf;

        [Display(Name = "Einleitung")]
        public string? Einleitung { get; set; }

        [Display(Name = "Schlusstext")]
        public string? Schlusstext { get; set; }

        [Display(Name = "Interne Notizen")]
        public string? Notizen { get; set; }

        [Display(Name = "MwSt. (%)")]
        public decimal MwStSatz { get; set; } = 19m;

        [Display(Name = "Bezahlt am")]
        [DataType(DataType.Date)]
        public DateTime? BezahltAm { get; set; }

        [Display(Name = "Erstellt am")]
        public DateTime ErstelltAm { get; set; } = DateTime.Now;

        public ICollection<RechnungPosition> Positionen { get; set; } = new List<RechnungPosition>();

        public decimal Nettobetrag  => Positionen.Sum(p => p.Gesamtpreis);
        public decimal MwStBetrag   => Nettobetrag * (MwStSatz / 100m);
        public decimal Bruttobetrag => Nettobetrag + MwStBetrag;
        public bool IstUeberfaellig => Status == RechnungStatus.Versendet
                                       && FaelligAm.HasValue && FaelligAm.Value < DateTime.Today;
    }

    public class RechnungPosition
    {
        public int Id { get; set; }
        public int RechnungId { get; set; }
        public Rechnung? Rechnung { get; set; }

        [Display(Name = "Pos.")]
        public int Position { get; set; }

        [Required]
        [Display(Name = "Bezeichnung")]
        [StringLength(500)]
        public string Bezeichnung { get; set; } = string.Empty;

        [Display(Name = "Beschreibung")]
        public string? Beschreibung { get; set; }

        [Display(Name = "Menge")]
        public decimal Menge { get; set; } = 1m;

        [Display(Name = "Einheit")]
        [StringLength(20)]
        public string Einheit { get; set; } = "Std.";

        [Display(Name = "Einzelpreis (€)")]
        public decimal Einzelpreis { get; set; }

        [Display(Name = "Rabatt (%)")]
        public decimal Rabatt { get; set; }

        public decimal Gesamtpreis => Menge * Einzelpreis * (1m - Rabatt / 100m);
    }

    // ── Einstellung ───────────────────────────────────────────────
    public class Einstellung
    {
        public int Id { get; set; }
        [StringLength(100)] public string Schluessel { get; set; } = string.Empty;
        public string? Wert { get; set; }
    }

    // ── Enums ─────────────────────────────────────────────────────
    public enum AngebotStatus
    {
        Entwurf    = 0,
        Versendet  = 1,
        Angenommen = 2,
        Abgelehnt  = 3,
        Abgelaufen = 4
    }

    public enum RechnungStatus
    {
        Entwurf      = 0,
        Versendet    = 1,
        Bezahlt      = 2,
        Storniert    = 3,
        Ueberfaellig = 4
    }
}
