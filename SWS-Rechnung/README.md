# SWS-Rechnung

**CRM · Angebote · Rechnungen** für die **steinwald.soft GmbH**

Ein vollständiges Business-Management-System für Kundenverwaltung, Angebote und Rechnungen mit Export in Word (.docx), ZUGFeRD-PDF und XRechnung-XML.

---

## Technologie-Stack

| Komponente | Technologie |
|---|---|
| Framework | ASP.NET MVC, .NET 8 |
| Frontend | Bootstrap 5.3, Bootstrap Icons |
| Datenbank | SQLite (via Entity Framework Core 8) |
| PDF-Export | iText7 8.0.4 |
| Word-Export | DocumentFormat.OpenXml 3.1.0 |
| E-Rechnung | ZUGFeRD 2.3 / XRechnung 3.x (CII EN 16931) |
| Schrift (Web) | DM Sans, DM Mono (Google Fonts) |

---

## Funktionsumfang

### Kundenverwaltung (CRM)
- Anlegen, Bearbeiten, Suchen und Filtern von Kunden
- Automatische Kundennummern (`KD-0001`)
- Aktiv/Inaktiv-Status
- Übersicht aller Angebote und Rechnungen je Kunde
- Löschen (nur wenn keine Dokumente vorhanden)

### Angebote
- Erstellen mit automatischer Angebotsnummer (`ANG-2026-0001`)
- Positionserfassung mit Menge, Einheit, Einzelpreis, Rabatt
- Live-Summenberechnung im Browser
- Status-Workflow: Entwurf → Versendet → Angenommen / Abgelehnt / Abgelaufen
- **Angebot als Rechnung übernehmen** (1-Klick-Konvertierung)
- Export als **Word (.docx)**

### Rechnungen
- Erstellen mit automatischer Rechnungsnummer (`RE-2026-0001`)
- Leistungszeitraum, Zahlungsziel, Fälligkeitsdatum
- Status-Workflow: Entwurf → Versendet → Bezahlt / Überfällig / Storniert
- Automatische Erkennung überfälliger Rechnungen
- Export als **Word (.docx)**
- Export als **ZUGFeRD 2.3 PDF** (visuell + maschinenlesbar)
- Export als **XRechnung XML** (CII-Syntax, für öffentliche Auftraggeber)

### Dashboard
- KPI-Kacheln: Kunden, offene Angebote, offene Rechnungen, Überfällige
- Umsatz aktueller Monat und Gesamtumsatz
- Überfällige Rechnungen mit Verzugstagen
- Letzte Angebote und Rechnungen

### Einstellungen
- Firmenstammdaten (Name, Adresse, Kontakt, USt-IdNr., Handelsregister)
- Bankverbindung (IBAN, BIC, Bankname)
- Nummernkreise und Präfixe für Angebote/Rechnungen
- Zahlungsziel (Standard)
- Standardtexte für Einleitung und Schlusstext

---

## Export-Formate

| Button | Format | Verwendung |
|---|---|---|
| 🔵 **Word** | `.docx` | Bearbeitung, E-Mail-Versand |
| 🟣 **ZUGFeRD** | PDF + eingebettetes XML | B2B, Archivierung, Buchhaltungssoftware |
| 🟢 **XRechnung** | CII-XML | Öffentliche Auftraggeber (Pflicht ab 2025) |

### Dokument-Layout (Word + ZUGFeRD-PDF)
- Firmenlogo zentriert in der Kopfzeile (6 cm breit)
- Grüne RECHNUNG/ANGEBOT-Box oben rechts
- Absender-Kurzzeile + Empfängeradresse links, Metadaten rechts
- Positionen-Tabelle mit blauem Header
- Summenblock mit blauem Gesamtbetrag-Balken
- Fußzeile: Seitenzahl + Firmeninfos (drei Zeilen zentriert)

---

## Voraussetzungen

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- Windows, macOS oder Linux
- Kein separater Datenbankserver erforderlich (SQLite)

---

## Installation & Start

```bash
# 1. ZIP entpacken und in das Verzeichnis wechseln
cd SWS-Rechnung

# 2. NuGet-Pakete wiederherstellen
dotnet restore

# 3. Anwendung starten
dotnet run
```

Die Datenbank (`sws_rechnung.db`) wird beim ersten Start **automatisch erstellt** und mit den Stammdaten der steinwald.soft GmbH befüllt.

Anschließend im Browser öffnen:
- **https://localhost:5001** (HTTPS)
- **http://localhost:5000** (HTTP)

---

## Projektstruktur

```
SWS-Rechnung.sln
SWS-Rechnung/
├── Controllers/
│   ├── HomeController.cs          # Dashboard + Einstellungen
│   ├── KundenController.cs        # CRM
│   ├── AngeboteController.cs      # Angebote + Word-Export
│   └── RechnungenController.cs    # Rechnungen + Word/ZUGFeRD/XRechnung
├── Data/
│   └── AppDbContext.cs            # EF Core DbContext + Seed-Daten
├── Infrastructure/
│   └── InvariantDecimalModelBinder.cs  # Dezimalzahlen kulturunabhängig binden
├── Migrations/                    # EF Core Datenbankmigrationen
├── Models/
│   ├── Models.cs                  # Domain-Modelle + Enums
│   └── PosEditorItem.cs           # ViewModel für Positions-Editor
├── Services/
│   ├── BaseServices.cs            # NummernService, EinstellungenService
│   ├── DocxService.cs             # Word-Export (.docx)
│   ├── PdfService.cs              # ZUGFeRD-PDF-Export (iText7)
│   └── ZugferdService.cs          # XRechnung/ZUGFeRD XML (CII EN 16931)
├── Views/
│   ├── Home/                      # Dashboard, Einstellungen
│   ├── Kunden/                    # CRM-Views
│   ├── Angebote/                  # Angebots-Views
│   ├── Rechnungen/                # Rechnungs-Views
│   └── Shared/                    # Layout, Positions-Editor, Status-Badges
├── wwwroot/
│   ├── css/site.css               # Marken-Stylesheet (Bootstrap-Erweiterung)
│   ├── js/site.js                 # Positions-Editor, Live-Summen
│   └── images/logo_gmbh.jpg       # Firmenlogo für Dokument-Export
├── Program.cs
├── appsettings.json
└── SWS-Rechnung.csproj
```

---

## Nummernformate

| Typ | Format | Beispiel |
|---|---|---|
| Kundennummer | `KD-XXXX` | `KD-0001` |
| Angebotsnummer | `ANG-JJJJ-XXXX` | `ANG-2026-0001` |
| Rechnungsnummer | `RE-JJJJ-XXXX` | `RE-2026-0001` |

Präfixe und Startnummern sind unter **Einstellungen** konfigurierbar.

---

## Farbpalette

| Farbe | Hex | Verwendung |
|---|---|---|
| Blau | `#0070C0` | Navigation, Tabellen-Header, Metadaten |
| Grün | `#B0C000` | RECHNUNG/ANGEBOT-Box, Akzentfarbe |
| Silber | `#C0C0C0` | Rahmen, dezente Elemente |
| Weiß | `#FFFFFF` | Hintergründe |
| Dunkel | `#1E1E1E` | Fließtext |

---

## Hinweise

**Dezimalzahlen:** `<input type="number">` sendet im Browser immer einen Punkt als Dezimaltrennzeichen. Ein Custom Model Binder (`InvariantDecimalModelBinder`) sorgt dafür, dass dies unabhängig von der Server-Kultur (z. B. `de-DE`) korrekt verarbeitet wird.

**Logo:** Das Firmenlogo (`wwwroot/images/logo_gmbh.jpg`) wird automatisch in Kopfzeilen von Word- und PDF-Dokumenten eingebettet. Wird die Datei nicht gefunden, erscheint der Firmenname als Text.

**MwSt.:** Der Standard-MwSt.-Satz ist `0 %` (§ 19 UStG – Kleinunternehmerregelung). Der Satz ist pro Dokument änderbar.

**E-Rechnung:** Das ZUGFeRD-PDF enthält ein eingebettetes `factur-x.xml` nach EN 16931. Das XRechnung-XML verwendet dieselbe CII-Syntax und ist direkt an Behörden und öffentliche Auftraggeber übermittelbar.

---

## Lizenz

Proprietär – steinwald.soft GmbH
