using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
namespace SWSRechnung.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Einstellungen",
                columns: table => new
                {
                    Id         = table.Column<int>(type: "INTEGER", nullable: false)
                                      .Annotation("Sqlite:Autoincrement", true),
                    Schluessel = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    Wert       = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Einstellungen", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Kunden",
                columns: table => new
                {
                    Id              = table.Column<int>(type: "INTEGER", nullable: false)
                                          .Annotation("Sqlite:Autoincrement", true),
                    Firmenname      = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Ansprechpartner = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Anrede          = table.Column<string>(type: "TEXT", nullable: true),
                    Strasse         = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    PLZ             = table.Column<string>(type: "TEXT", maxLength: 10, nullable: true),
                    Ort             = table.Column<string>(type: "TEXT", maxLength: 100, nullable: true),
                    Land            = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false, defaultValue: "Deutschland"),
                    Telefon         = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Email           = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    Website         = table.Column<string>(type: "TEXT", maxLength: 200, nullable: true),
                    UstIdNr         = table.Column<string>(type: "TEXT", maxLength: 50, nullable: true),
                    Kundennummer    = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Notizen         = table.Column<string>(type: "TEXT", nullable: true),
                    ErstelltAm      = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Aktiv           = table.Column<bool>(type: "INTEGER", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Kunden", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Angebote",
                columns: table => new
                {
                    Id             = table.Column<int>(type: "INTEGER", nullable: false)
                                         .Annotation("Sqlite:Autoincrement", true),
                    Angebotsnummer = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    KundeId        = table.Column<int>(type: "INTEGER", nullable: false),
                    Betreff        = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Angebotsdatum  = table.Column<DateTime>(type: "TEXT", nullable: false),
                    GueltigBis     = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status         = table.Column<int>(type: "INTEGER", nullable: false),
                    Einleitung     = table.Column<string>(type: "TEXT", nullable: true),
                    Schlusstext    = table.Column<string>(type: "TEXT", nullable: true),
                    Notizen        = table.Column<string>(type: "TEXT", nullable: true),
                    MwStSatz       = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    ErstelltAm     = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Angebote", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Angebote_Kunden_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunden",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Rechnungen",
                columns: table => new
                {
                    Id              = table.Column<int>(type: "INTEGER", nullable: false)
                                          .Annotation("Sqlite:Autoincrement", true),
                    Rechnungsnummer = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    KundeId         = table.Column<int>(type: "INTEGER", nullable: false),
                    AngebotId       = table.Column<int>(type: "INTEGER", nullable: true),
                    Betreff         = table.Column<string>(type: "TEXT", maxLength: 300, nullable: true),
                    Rechnungsdatum  = table.Column<DateTime>(type: "TEXT", nullable: false),
                    LeistungVon     = table.Column<DateTime>(type: "TEXT", nullable: true),
                    LeistungBis     = table.Column<DateTime>(type: "TEXT", nullable: true),
                    FaelligAm       = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Status          = table.Column<int>(type: "INTEGER", nullable: false),
                    Einleitung      = table.Column<string>(type: "TEXT", nullable: true),
                    Schlusstext     = table.Column<string>(type: "TEXT", nullable: true),
                    Notizen         = table.Column<string>(type: "TEXT", nullable: true),
                    MwStSatz        = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false),
                    BezahltAm       = table.Column<DateTime>(type: "TEXT", nullable: true),
                    ErstelltAm      = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Rechnungen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Rechnungen_Kunden_KundeId",
                        column: x => x.KundeId,
                        principalTable: "Kunden",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Rechnungen_Angebote_AngebotId",
                        column: x => x.AngebotId,
                        principalTable: "Angebote",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AngebotPositionen",
                columns: table => new
                {
                    Id           = table.Column<int>(type: "INTEGER", nullable: false)
                                       .Annotation("Sqlite:Autoincrement", true),
                    AngebotId    = table.Column<int>(type: "INTEGER", nullable: false),
                    Position     = table.Column<int>(type: "INTEGER", nullable: false),
                    Bezeichnung  = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                    Menge        = table.Column<decimal>(type: "TEXT", precision: 10, scale: 3, nullable: false),
                    Einheit      = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Einzelpreis  = table.Column<decimal>(type: "TEXT", precision: 12, scale: 4, nullable: false),
                    Rabatt       = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AngebotPositionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AngebotPositionen_Angebote_AngebotId",
                        column: x => x.AngebotId,
                        principalTable: "Angebote",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RechnungPositionen",
                columns: table => new
                {
                    Id           = table.Column<int>(type: "INTEGER", nullable: false)
                                       .Annotation("Sqlite:Autoincrement", true),
                    RechnungId   = table.Column<int>(type: "INTEGER", nullable: false),
                    Position     = table.Column<int>(type: "INTEGER", nullable: false),
                    Bezeichnung  = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Beschreibung = table.Column<string>(type: "TEXT", nullable: true),
                    Menge        = table.Column<decimal>(type: "TEXT", precision: 10, scale: 3, nullable: false),
                    Einheit      = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Einzelpreis  = table.Column<decimal>(type: "TEXT", precision: 12, scale: 4, nullable: false),
                    Rabatt       = table.Column<decimal>(type: "TEXT", precision: 5, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RechnungPositionen", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RechnungPositionen_Rechnungen_RechnungId",
                        column: x => x.RechnungId,
                        principalTable: "Rechnungen",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            // ── Indizes ───────────────────────────────────────────
            migrationBuilder.CreateIndex("IX_Angebote_KundeId",        "Angebote",          "KundeId");
            migrationBuilder.CreateIndex("IX_Rechnungen_KundeId",       "Rechnungen",        "KundeId");
            migrationBuilder.CreateIndex("IX_Rechnungen_AngebotId",     "Rechnungen",        "AngebotId");
            migrationBuilder.CreateIndex("IX_AngebotPositionen_AngId",  "AngebotPositionen", "AngebotId");
            migrationBuilder.CreateIndex("IX_RechnungPositionen_ReId",  "RechnungPositionen","RechnungId");

            // ── Seed: Einstellungen ────────────────────────────────
            migrationBuilder.InsertData(
                table: "Einstellungen",
                columns: new[] { "Id", "Schluessel", "Wert" },
                values: new object[,]
                {
                    { 1,  "FirmaName",           "steinwald.soft GmbH" },
                    { 2,  "FirmaStrasse",         "Steinwaldstraße 1" },
                    { 3,  "FirmaPLZ",             "12345" },
                    { 4,  "FirmaOrt",             "Musterstadt" },
                    { 5,  "FirmaLand",            "Deutschland" },
                    { 6,  "FirmaTelefon",         "+49 (0) 123 456 789" },
                    { 7,  "FirmaEmail",           "info@steinwald-soft.de" },
                    { 8,  "FirmaWebsite",         "www.steinwald-soft.de" },
                    { 9,  "FirmaUstId",           "DE123456789" },
                    { 10, "FirmaHandelsregister", "HRB 12345" },
                    { 11, "BankName",             "Musterbank AG" },
                    { 12, "BankIBAN",             "DE00 0000 0000 0000 0000 00" },
                    { 13, "BankBIC",              "MUSTDEXX" },
                    { 14, "AngebotPrefix",        "ANG" },
                    { 15, "RechnungPrefix",       "RE" },
                    { 16, "AngebotNaechsteNr",    "1" },
                    { 17, "RechnungNaechsteNr",   "1" },
                    { 18, "Zahlungsziel",         "14" },
                    { 19, "AngebotEinleitung",    "vielen Dank für Ihre Anfrage. Gerne unterbreiten wir Ihnen folgendes Angebot:" },
                    { 20, "AngebotSchlusstext",   "Wir freuen uns auf Ihre Rückmeldung und stehen für Fragen gerne zur Verfügung.\n\nMit freundlichen Grüßen\nsteinwald.soft GmbH" },
                    { 21, "RechnungEinleitung",   "wir erlauben uns, Ihnen folgende Leistungen in Rechnung zu stellen:" },
                    { 22, "RechnungSchlusstext",  "Bitte überweisen Sie den Rechnungsbetrag innerhalb von 14 Tagen auf das unten angegebene Konto.\n\nVielen Dank für Ihr Vertrauen.\n\nMit freundlichen Grüßen\nsteinwald.soft GmbH" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("AngebotPositionen");
            migrationBuilder.DropTable("RechnungPositionen");
            migrationBuilder.DropTable("Rechnungen");
            migrationBuilder.DropTable("Angebote");
            migrationBuilder.DropTable("Kunden");
            migrationBuilder.DropTable("Einstellungen");
        }
    }
}
