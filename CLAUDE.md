# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Quick Start

**Build & Run:**
```powershell
# From repo root (or from SWS-Rechnung/ subdirectory)
dotnet restore SWS-Rechnung/SWS-Rechnung.csproj
dotnet build SWS-Rechnung/SWS-Rechnung.csproj

# Run the development server
dotnet run --project SWS-Rechnung/SWS-Rechnung.csproj
# OR via Visual Studio: open SWS-Rechnung.sln and press F5

# Application will be available at:
# https://localhost:62272 (HTTPS)
# http://localhost:62273 (HTTP)
```

**Database:** SQLite database (`sws_rechnung.db`) is auto-created on first run with seed data (company settings for steinwald.soft GmbH).

## Architecture Overview

**SWS-Rechnung** is an ASP.NET Core MVC 8 business management system for customer relationships, quotes, and invoices. The application is a monolithic MVC structure with clear separation of concerns:

### Layered Architecture

1. **Controllers** (4 main controllers)
   - `HomeController`: Dashboard with KPIs and global settings management
   - `KundenController`: Customer (CRM) CRUD operations
   - `AngeboteController`: Quote management
   - `RechnungenController`: Invoice management with status workflow
   
2. **Data Layer** (`AppDbContext`)
   - Entity Framework Core 8 with SQLite
   - 6 DbSets: Kunden, Angebote, AngebotPositionen, Rechnungen, RechnungPositionen, Einstellungen
   - Relationship constraints: Customers can't be deleted if they have quotes/invoices (Restrict)
   - Position deletions cascade when parent document is deleted
   - Decimal precision configured explicitly (MwStSatz: 5,2; Menge: 10,3; Preise: 12,4)

3. **Services** (Dependency-injected via Program.cs)
   - `EinstellungenService`: Configuration key-value store (name, address, bank details, number prefixes, templates)
   - `NummernService`: Auto-generates sequential numbers (ANG-YYYY-XXXX, RE-YYYY-XXXX)
   - `PdfService`: iText7-based ZUGFeRD PDF generation (layout: logo in header, blue/green palette, footer with company info)
   - `DocxService`: DocumentFormat.OpenXml Word export (same layout as PDF)
   - `ZugferdService`: EN 16931 / XRechnung 3.x CII XML generation for invoices only

4. **Models** (Domain-driven)
   - **Kunde**: Customer with auto-generated KD-XXXX number
   - **Angebot** + **AngebotPosition**: Quote with status enum (Draft → Sent → Accepted/Rejected/Expired)
   - **Rechnung** + **RechnungPosition**: Invoice with extended status enum (Draft → Sent → Paid/Overdue/Cancelled)
   - **Einstellung**: Configuration KV store (seeded with 23 default settings)
   - **PosEditorItem**: Flat ViewModel for position editor (avoids dynamic in Razor)

### Critical Implementation Details

**Decimal Binding:** Custom `InvariantDecimalModelBinder` ensures `<input type="number">` fields (which always send a dot) are parsed correctly regardless of server culture (de-DE, etc.). Registered in Program.cs before other model binder providers.

**Number Generation:** Incremental counters stored in Einstellungen table. Each call to `NaechsteAngebotsnummerAsync()` or `NaechsteRechnungsnummerAsync()` increments the counter and formats as `{PREFIX}-{YEAR}-{PADDED_NUMBER}`.

**Dashboard Auto-Updates:** `RechnungenController.Index()` automatically marks invoices as "Überfällig" (Overdue) if Status=Versendet and FaelligAm < today. This runs on every page load.

**PDF/Word Layout:** Three export services share a common strategy:
- Logo (6cm wide) centered in header, fetched from `wwwroot/images/logo_gmbh.jpg`
- Type box (RECHNUNG/ANGEBOT) top-right in brand green
- Blue table headers and footer with page numbers
- Footer text includes "§19 UStG" disclaimer (small business exemption)

**MwSt Default:** Set to 0% (small business exemption) but is configurable per document.

## Project Structure

The solution root contains `CLAUDE.md`, `README.md`, and a single `SWS-Rechnung/` project directory:

```
SWS-Rechnung/               ← ASP.NET Core project
├── Controllers/             # MVC controllers (4 classes)
├── Data/
│   └── AppDbContext.cs      # DbContext + Fluent Config + Seed Data
├── Infrastructure/
│   └── InvariantDecimalModelBinder.cs  # Custom decimal parser
├── Migrations/              # EF Core migration history
├── Models/
│   ├── Models.cs            # Kunde, Angebot, Rechnung, Einstellung + Enums
│   └── PosEditorItem.cs     # ViewModel for position editor UI
├── Services/                # Business logic + exports
├── Views/                   # Razor templates (MVC)
│   ├── Home/                # Dashboard, Settings
│   ├── Kunden/              # Customer CRUD
│   ├── Angebote/            # Quote CRUD
│   ├── Rechnungen/          # Invoice CRUD
│   └── Shared/              # _Layout.cshtml, _PosEditor.cshtml, _StatusBadge.cshtml
├── wwwroot/
│   ├── css/site.css         # Bootstrap 5 + brand overrides
│   ├── js/site.js           # Position editor, live sum calculations
│   └── images/logo_gmbh.jpg # Company logo for exports
├── appsettings.json         # Connection string, logging
├── Program.cs               # DI setup, middleware pipeline
└── SWS-Rechnung.csproj      # .NET 8, package references
```

## Key Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft.EntityFrameworkCore.Sqlite` | 8.0.0 | ORM + SQLite driver |
| `Microsoft.EntityFrameworkCore.Tools` | 8.0.0 | Migrations CLI |
| `itext7` | 8.0.4 | PDF generation (ZUGFeRD) |
| `itext7.bouncy-castle-adapter` | 8.0.4 | PDF signing support |
| `DocumentFormat.OpenXml` | 3.1.0 | Word document generation |
| Bootstrap 5.3.3 | CDN | Frontend framework |

## Common Development Tasks

**Add a new document export format:**
1. Create a new service in `Services/` implementing export logic
2. Inject it in `Program.cs` via `builder.Services.AddScoped<YourService>()`
3. Add action methods in `RechnungenController` or `AngeboteController` (naming: `ExportFormat`)
4. Add export buttons in `Views/Rechnungen/Details.cshtml` or equivalent
5. If it's a text format (XML, JSON), return via `File()` or redirect; if binary (PDF/docx), use `FileContentResult`

**Modify document layout (PDF/Word):**
- **PdfService.cs**: Layout is built in `Build()` method; modify margins, colors, or table widths
- **DocxService.cs**: Modify MARG_* constants for page margins, BLAU/GRUEN/GRAU color codes
- **Both services load logo from `wwwroot/images/logo_gmbh.jpg`** — if missing, they fall back to text

**Add a new configuration setting:**
1. Insert a row in AppDbContext seed data (OnModelCreating) with `Schluessel="MyKey"` and default `Wert`
2. Call `await _einst.GetAsync("MyKey", "fallback")` in controllers/services
3. Settings form in `Home/Einstellungen.cshtml` automatically renders all Einstellung records

**Create a new data migration:**
```powershell
cd SWS-Rechnung
dotnet ef migrations add MyMigrationName
dotnet ef database update
```

**Status Workflow Transitions:**
- **Angebot**: Entwurf (0) → Versendet (1) → Angenommen (2) / Abgelehnt (3) / Abgelaufen (4)
- **Rechnung**: Entwurf (0) → Versendet (1) → Bezahlt (2) / Überfällig (4) / Storniert (3)
  - *Note:* Überfällig is auto-set by dashboard; clients should update to Bezahlt manually

## Decimal Number Handling

The application is German-localized (de-DE culture). HTML form inputs use `<input type="number">` which always send decimal values with a dot (`.`) regardless of browser locale. The custom `InvariantDecimalModelBinder` normalizes commas to dots and parses with `CultureInfo.InvariantCulture`. This means:
- Model properties are `decimal` or `decimal?`
- Displayed values in views use `@item.Price.ToString("F2")` (de-DE formatting applied by runtime)
- No manual culture switching needed in services

## Export Formats at a Glance

| Format | Service | Output | Key Features |
|--------|---------|--------|--------------|
| Word (.docx) | `DocxService` | Binary file | Editable, logo, brand colors |
| ZUGFeRD PDF | `PdfService` | PDF + embedded XML | Factur-X compliant, machine-readable invoices |
| XRechnung XML | `ZugferdService` | Plain XML (CII) | For German public sector (§ 30 UStG) |

**When to use:**
- **Word**: Internal editing, email templates
- **ZUGFeRD PDF**: B2B, archival, bookkeeping software import
- **XRechnung**: Mandatory for public sector invoices (B2G)

## UI Patterns & Views

- **_Layout.cshtml**: Brand navigation (logo, menu, "SWS" mark + tagline), Bootstrap 5 grid
- **_PosEditor.cshtml**: Shared position-line-item editor for both quotes and invoices (dynamic row addition via JS)
- **_StatusBadge.cshtml**: Color-coded status badge (Entwurf=gray, Versendet=blue, Bezahlt=green, etc.)
- **Live Sum Calculations**: `site.js` watches position inputs (Menge, Einzelpreis, Rabatt) and updates totals without server call
- **Confirmation Dialogs**: Delete/status-change operations use `if (confirm("...")) { ... }`

## Considerations for Changes

1. **Cascading Deletes**: Positions are deleted when parent (Angebot/Rechnung) is deleted. Kunden have Restrict (can't delete if linked).
2. **Decimal Precision**: Explicitly configured in AppDbContext migrations. Changing scale (e.g., from 4 to 5) requires a new migration.
3. **Number Generation**: Relies on Einstellungen being seeded. If a customer manually deletes or corrupts a number entry, subsequent calls will fail (handle gracefully).
4. **Culture Sensitivity**: All date formatting uses `.ToString("dd.MM.yyyy")` explicitly (not culture-based). XML/PDF timestamps are ISO 8601 (invariant).
5. **Performance**: Dashboard runs a full Rechnungen query to check for overdue invoices on every page load. With large datasets (>10k invoices), consider caching or background job.

## Testing Notes

- **No automated tests present** — manual QA or integration testing recommended
- **Database is local SQLite** — no migration/seed issues across environments; `sws_rechnung.db` is gitignored
- **No authentication/authorization** — application assumes single-user or trusted network; add auth if exposed
