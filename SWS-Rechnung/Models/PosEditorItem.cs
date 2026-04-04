namespace SWSRechnung.Models
{
    /// <summary>
    /// Flaches ViewModel für den Positions-Editor – vermeidet dynamic in Razor.
    /// </summary>
    public class PosEditorItem
    {
        public int     Id           { get; set; }
        public int     Position     { get; set; }
        public string  Bezeichnung  { get; set; } = string.Empty;
        public string? Beschreibung { get; set; }
        public decimal Menge        { get; set; } = 1m;
        public string  Einheit      { get; set; } = "Std.";
        public decimal Einzelpreis  { get; set; }
        public decimal Rabatt       { get; set; }
        public decimal Gesamtpreis  => Menge * Einzelpreis * (1m - Rabatt / 100m);

        // Factory helpers
        public static PosEditorItem From(AngebotPosition p) => new()
        {
            Id = p.Id, Position = p.Position, Bezeichnung = p.Bezeichnung,
            Beschreibung = p.Beschreibung, Menge = p.Menge, Einheit = p.Einheit,
            Einzelpreis = p.Einzelpreis, Rabatt = p.Rabatt
        };
        public static PosEditorItem From(RechnungPosition p) => new()
        {
            Id = p.Id, Position = p.Position, Bezeichnung = p.Bezeichnung,
            Beschreibung = p.Beschreibung, Menge = p.Menge, Einheit = p.Einheit,
            Einzelpreis = p.Einzelpreis, Rabatt = p.Rabatt
        };
    }
}
