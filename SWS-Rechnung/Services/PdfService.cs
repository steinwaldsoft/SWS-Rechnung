using System.Globalization;
using iText.IO.Image;
using iText.Kernel.Colors;
using iText.Kernel.Events;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas;
using iText.IO.Font.Constants;
using iText.Layout;
using iText.Layout.Borders;
using iText.Layout.Element;
using iText.Layout.Properties;
using SWSRechnung.Models;

namespace SWSRechnung.Services
{
    public class PdfService
    {
        private readonly EinstellungenService _einst;
        private readonly IWebHostEnvironment  _env;

        private static readonly DeviceRgb Blau     = new(  0, 112, 192);
        private static readonly DeviceRgb Gruen    = new(176, 192,   0);
        private static readonly DeviceRgb Grau     = new(140, 140, 140);
        private static readonly DeviceRgb HellGrau = new(220, 220, 220);
        private static readonly DeviceRgb HellBg   = new(235, 244, 251);
        private static readonly DeviceRgb Dunkel   = new( 30,  30,  30);

        private static readonly CultureInfo DE = new("de-DE");

        public PdfService(EinstellungenService einst, IWebHostEnvironment env)
        { _einst = einst; _env = env; }

        // ── Öffentliche API ───────────────────────────────────────────

        public async Task<byte[]> AngebotPdfAsync(Angebot a)
        {
            var e = await _einst.GetAllAsync();
            return Build(e, "ANGEBOT", a.Angebotsnummer, a.Angebotsdatum,
                "Gültig bis", a.GueltigBis?.ToString("dd.MM.yyyy") ?? "–", false,
                a.Kunde?.Kundennummer ?? "", a.Kunde, a.Betreff,
                a.Einleitung, a.Schlusstext, a.MwStSatz,
                a.Nettobetrag, a.MwStBetrag, a.Bruttobetrag,
                a.Positionen.OrderBy(p=>p.Position)
                 .Select(p=>(p.Position,p.Bezeichnung,p.Beschreibung,
                             p.Menge,p.Einheit,p.Einzelpreis,p.Rabatt,p.Gesamtpreis))
                 .ToList());
        }

        public async Task<byte[]> RechnungPdfAsync(Rechnung r)
        {
            var e = await _einst.GetAllAsync();
            return Build(e, "RECHNUNG", r.Rechnungsnummer, r.Rechnungsdatum,
                "Zahlbar bis", r.FaelligAm?.ToString("dd.MM.yyyy") ?? "–", true,
                r.Kunde?.Kundennummer ?? "", r.Kunde, r.Betreff,
                r.Einleitung, r.Schlusstext, r.MwStSatz,
                r.Nettobetrag, r.MwStBetrag, r.Bruttobetrag,
                r.Positionen.OrderBy(p=>p.Position)
                 .Select(p=>(p.Position,p.Bezeichnung,p.Beschreibung,
                             p.Menge,p.Einheit,p.Einzelpreis,p.Rabatt,p.Gesamtpreis))
                 .ToList());
        }

        // ── Kern-Builder ──────────────────────────────────────────────

        private byte[] Build(
            Dictionary<string,string> e,
            string typ, string nummer, DateTime datum,
            string extraLabel, string extraValue, bool extraBlau,
            string kundennr, Kunde? kunde,
            string? betreff, string? einleitung, string? schlusstext,
            decimal mwstSatz, decimal netto, decimal mwst, decimal brutto,
            List<(int Pos, string Bez, string? Beschr,
                  decimal Menge, string Einheit,
                  decimal EP, decimal Rabatt, decimal GP)> positionen)
        {
            using var ms  = new MemoryStream();
            var pdfWriter = new PdfWriter(ms);
            var pdfDoc    = new PdfDocument(pdfWriter);
            var pageSize  = PageSize.A4;

            // Seitenränder: oben 120pt (Platz für Logo), unten 100pt (Fußzeile)
            float mL = 70f, mR = 70f, mT = 95f, mB = 100f;

            var doc = new Document(pdfDoc, pageSize);
            doc.SetMargins(mT, mR, mB, mL);

            var bold    = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var regular = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            byte[]? logo = LoadLogo();
            pdfDoc.AddEventHandler(PdfDocumentEvent.END_PAGE,
                new PageHandler(logo, e, bold, regular, pageSize));

            // ── Typ-Box (RECHNUNG/ANGEBOT) oben rechts ───────────
            // Feste Breite, rechtsbündig – wie im Word-Dokument
            float boxW = 160f; // ca. 5,6 cm – nur so breit wie der Text
            var typTbl = new Table(new float[]{ doc.GetPageEffectiveArea(pageSize).GetWidth() - boxW, boxW })
                .SetWidth(UnitValue.CreatePercentValue(100))
                .SetMarginBottom(12f);
            typTbl.AddCell(new Cell().SetBorder(Border.NO_BORDER));
            var tc = new Cell().SetBackgroundColor(Gruen).SetBorder(Border.NO_BORDER)
                .SetPaddingTop(8f).SetPaddingBottom(8f).SetPaddingLeft(14f).SetPaddingRight(14f);
            tc.Add(new Paragraph(typ).SetFont(bold).SetFontSize(18f)
                .SetFontColor(ColorConstants.WHITE)
                .SetTextAlignment(TextAlignment.RIGHT).SetMarginBottom(2f));
            tc.Add(new Paragraph(nummer).SetFont(regular).SetFontSize(9f)
                .SetFontColor(ColorConstants.WHITE).SetTextAlignment(TextAlignment.RIGHT));
            typTbl.AddCell(tc);
            doc.Add(typTbl);

            // ── Adresse | Metadaten ───────────────────────────────
            var addrTbl = new Table(new float[]{3f, 2f}).UseAllAvailableWidth()
                .SetMarginBottom(16f);

            var addrCell = new Cell().SetBorder(Border.NO_BORDER);
            addrCell.Add(new Paragraph(
                e.G("FirmaName") + "  \u00B7  " + e.G("FirmaStrasse") +
                "  \u00B7  " + e.G("FirmaPLZ") + " " + e.G("FirmaOrt"))
                .SetFont(regular).SetFontSize(7.5f).SetFontColor(Grau)
                .SetUnderline().SetMarginBottom(7f));
            if (kunde != null)
            {
                addrCell.Add(new Paragraph(kunde.Firmenname)
                    .SetFont(bold).SetFontSize(11f).SetFontColor(Dunkel).SetMarginBottom(2f));
                if (!string.IsNullOrEmpty(kunde.Ansprechpartner))
                    addrCell.Add(new Paragraph(
                        ((kunde.Anrede ?? "") + " " + kunde.Ansprechpartner).Trim())
                        .SetFont(regular).SetFontSize(10f).SetMarginBottom(1f));
                if (!string.IsNullOrEmpty(kunde.Strasse))
                    addrCell.Add(new Paragraph(kunde.Strasse)
                        .SetFont(regular).SetFontSize(10f).SetMarginBottom(1f));
                addrCell.Add(new Paragraph((kunde.PLZ ?? "") + " " + (kunde.Ort ?? ""))
                    .SetFont(regular).SetFontSize(10f));
                if (!string.IsNullOrEmpty(kunde.Land)
                    && kunde.Land != "Deutschland"
                    && kunde.Land != "DE")
                    addrCell.Add(new Paragraph(kunde.Land)
                        .SetFont(regular).SetFontSize(10f));
            }
            addrTbl.AddCell(addrCell);

            var metaCell = new Cell().SetBorder(Border.NO_BORDER);
            void Meta(string lbl, string val, bool vBold = false, bool vBlau = false)
            {
                metaCell.Add(new Paragraph(lbl).SetFont(bold).SetFontSize(8f)
                    .SetFontColor(Grau).SetTextAlignment(TextAlignment.RIGHT).SetMarginBottom(0f));
                metaCell.Add(new Paragraph(val)
                    .SetFont(vBold ? bold : regular).SetFontSize(10f)
                    .SetFontColor(vBlau ? (iText.Kernel.Colors.Color)Blau : Dunkel)
                    .SetTextAlignment(TextAlignment.RIGHT).SetMarginBottom(6f));
            }
            Meta("Datum",       datum.ToString("dd.MM.yyyy"));
            if (!string.IsNullOrEmpty(kundennr))
                Meta("Kundennummer", kundennr);
            Meta(extraLabel, extraValue, vBold: extraBlau, vBlau: extraBlau);
            addrTbl.AddCell(metaCell);
            doc.Add(addrTbl);

            // ── Betreff ───────────────────────────────────────────
            if (!string.IsNullOrEmpty(betreff))
                doc.Add(new Paragraph(betreff).SetFont(bold).SetFontSize(11f)
                    .SetFontColor(Blau).SetMarginBottom(8f));

            // ── Anrede + Einleitung ───────────────────────────────
            string anrede = "Sehr geehrte Damen und Herren,";
            if (kunde?.Ansprechpartner != null)
                anrede = kunde.Anrede switch {
                    "Herr" => $"Sehr geehrter Herr {kunde.Ansprechpartner},",
                    "Frau" => $"Sehr geehrte Frau {kunde.Ansprechpartner},",
                    _      => $"Sehr geehrte/r {kunde.Ansprechpartner},"
                };
            doc.Add(new Paragraph(anrede).SetFont(regular).SetFontSize(10f).SetMarginBottom(4f));
            if (!string.IsNullOrEmpty(einleitung))
                doc.Add(new Paragraph(einleitung).SetFont(regular).SetFontSize(10f).SetMarginBottom(10f));

            // ── Positions-Tabelle ─────────────────────────────────
            var pt = new Table(new float[]{0.45f, 3.6f, 0.75f, 0.65f, 1.35f, 0.55f, 1.65f})
                .UseAllAvailableWidth().SetMarginBottom(6f);

            void Th(string text, TextAlignment al = TextAlignment.LEFT)
                => pt.AddHeaderCell(new Cell()
                    .SetBackgroundColor(Blau).SetBorder(Border.NO_BORDER)
                    .SetPaddingTop(3f).SetPaddingBottom(3f)
                    .SetPaddingLeft(4f).SetPaddingRight(4f)
                    .Add(new Paragraph(text).SetFont(bold).SetFontSize(8.5f)
                        .SetFontColor(ColorConstants.WHITE).SetTextAlignment(al)));
            Th("Pos.",     TextAlignment.CENTER);
            Th("Bezeichnung");
            Th("Menge",    TextAlignment.RIGHT);
            Th("Einh.",    TextAlignment.CENTER);
            Th("Einzelpr.",TextAlignment.RIGHT);
            Th("Rbt%",     TextAlignment.RIGHT);
            Th("Gesamt",   TextAlignment.RIGHT);

            bool alt = false;
            foreach (var p in positionen)
            {
                var bg = alt ? HellBg : ColorConstants.WHITE;
                Cell DC(string text, bool iBold=false, TextAlignment al=TextAlignment.LEFT)
                    => new Cell()
                        .SetBackgroundColor(bg)
                        .SetBorderTop(new SolidBorder(HellGrau, 0.5f))
                        .SetBorderBottom(Border.NO_BORDER)
                        .SetBorderLeft(Border.NO_BORDER).SetBorderRight(Border.NO_BORDER)
                        .SetPaddingTop(5f).SetPaddingBottom(5f)
                        .SetPaddingLeft(4f).SetPaddingRight(4f)
                        .Add(new Paragraph(text)
                            .SetFont(iBold?bold:regular).SetFontSize(9f).SetTextAlignment(al));

                pt.AddCell(DC(p.Pos.ToString(), al: TextAlignment.CENTER));
                var bc = new Cell().SetBackgroundColor(bg)
                    .SetBorderTop(new SolidBorder(HellGrau,0.5f))
                    .SetBorderBottom(Border.NO_BORDER)
                    .SetBorderLeft(Border.NO_BORDER).SetBorderRight(Border.NO_BORDER)
                    .SetPaddingTop(5f).SetPaddingBottom(5f)
                    .SetPaddingLeft(4f).SetPaddingRight(4f);
                bc.Add(new Paragraph(p.Bez).SetFont(bold).SetFontSize(9f).SetFontColor(Dunkel));
                if (!string.IsNullOrEmpty(p.Beschr))
                    bc.Add(new Paragraph(p.Beschr).SetFont(regular).SetFontSize(8f).SetFontColor(Grau));
                pt.AddCell(bc);
                pt.AddCell(DC(p.Menge.ToString("N2",DE),         al:TextAlignment.RIGHT));
                pt.AddCell(DC(p.Einheit,                          al:TextAlignment.CENTER));
                pt.AddCell(DC(p.EP.ToString("N2",DE)+" \u20AC",  al:TextAlignment.RIGHT));
                pt.AddCell(DC(p.Rabatt>0?p.Rabatt.ToString("N1",DE)+"%":"\u2013",
                                                                  al:TextAlignment.RIGHT));
                pt.AddCell(DC(p.GP.ToString("N2",DE)+" \u20AC",  iBold:true, al:TextAlignment.RIGHT));
                alt = !alt;
            }
            doc.Add(pt);

            // ── Summen ────────────────────────────────────────────
            var st = new Table(new float[]{5f,2f}).UseAllAvailableWidth().SetMarginBottom(14f);
            void SR(string lbl, string val, bool tot=false)
            {
                var bg  = tot ? Blau : ColorConstants.WHITE;
                var fc  = tot ? (iText.Kernel.Colors.Color)ColorConstants.WHITE : Dunkel;
                var fnt = tot ? bold : regular;
                float fs = tot ? 10.5f : 9.5f;
                float pd = tot ? 3f : 4f;
                st.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetBackgroundColor(bg)
                    .SetPaddingTop(pd).SetPaddingBottom(pd).SetPaddingLeft(6f).SetPaddingRight(6f)
                    .Add(new Paragraph(lbl).SetFont(fnt).SetFontSize(fs)
                        .SetFontColor(fc).SetTextAlignment(TextAlignment.RIGHT)));
                st.AddCell(new Cell().SetBorder(Border.NO_BORDER).SetBackgroundColor(bg)
                    .SetPaddingTop(pd).SetPaddingBottom(pd).SetPaddingLeft(6f).SetPaddingRight(6f)
                    .Add(new Paragraph(val).SetFont(fnt).SetFontSize(fs)
                        .SetFontColor(fc).SetTextAlignment(TextAlignment.RIGHT)));
            }
            SR("Nettobetrag:",              netto.ToString("N2",DE)+" \u20AC");
            SR($"MwSt. ({mwstSatz:N0} %):", mwst.ToString("N2",DE)+" \u20AC");
            SR("Gesamtbetrag:",             brutto.ToString("N2",DE)+" \u20AC", tot:true);
            doc.Add(st);

            // ── Schlusstext ───────────────────────────────────────
            if (!string.IsNullOrEmpty(schlusstext))
                foreach (var line in schlusstext.Split('\n'))
                    doc.Add(new Paragraph(line.TrimEnd('\r'))
                        .SetFont(regular).SetFontSize(10f).SetMarginBottom(3f));

            doc.Close();
            return ms.ToArray();
        }

        // ── Page Event Handler: Logo + Fußzeile ───────────────────────

        private class PageHandler : IEventHandler
        {
            private readonly byte[]?                   _logo;
            private readonly Dictionary<string,string> _e;
            private readonly PdfFont _bold, _regular;
            private readonly PageSize _page;

            public PageHandler(byte[]? logo, Dictionary<string,string> e,
                PdfFont bold, PdfFont regular, PageSize page)
            { _logo=logo; _e=e; _bold=bold; _regular=regular; _page=page; }

            public void HandleEvent(Event ev)
            {
                var docEv  = (PdfDocumentEvent)ev;
                var pdfDoc = docEv.GetDocument();
                var page   = docEv.GetPage();
                float w    = _page.GetWidth();
                float h    = _page.GetHeight();
                float mL   = 70f, mR = 70f;

                // ── Logo zentriert in Kopfzeile ──────────────────
                if (_logo != null)
                {
                    try
                    {
                        var canvas2 = new PdfCanvas(
                            page.NewContentStreamBefore(), page.GetResources(), pdfDoc);
                        float logoW = 170f;
                        float logoH = logoW * 386f / 1545f;
                        float logoX = (w - logoW) / 2f;
                        float logoY = h - 10f - logoH;
                        var imgData = ImageDataFactory.Create(_logo);
                        new iText.Layout.Canvas(canvas2,
                                new Rectangle(logoX, logoY, logoW, logoH))
                            .Add(new Image(imgData).SetWidth(logoW).SetHeight(logoH))
                            .Close();
                        canvas2.Release();
                    }
                    catch { /* kein Logo – kein Absturz */ }
                }

                // ── Fußzeile ─────────────────────────────────────
                var canvas = new PdfCanvas(
                    page.NewContentStreamAfter(), page.GetResources(), pdfDoc);

                int pgNr    = pdfDoc.GetPageNumber(page);
                int pgCount = pdfDoc.GetNumberOfPages();
                string pgTxt = $"Seite {pgNr} von {pgCount}";
                var fGrau = new DeviceRgb(140, 140, 140);

                // Seitenzahl rechtsbündig
                float pgW = _regular.GetWidth(pgTxt, 8f);
                canvas.BeginText()
                    .SetFontAndSize(_regular, 8f).SetColor(fGrau, true)
                    .MoveText(w - mR - pgW, 68f)
                    .ShowText(pgTxt).EndText();

                // Drei Zeilen Firmeninfo zentriert
                string l1 = _e.G("FirmaName") + "  \u00B7  " + _e.G("FirmaStrasse") +
                            "  \u00B7  " + _e.G("FirmaPLZ") + " " + _e.G("FirmaOrt") +
                            "  \u00B7  Tel: " + _e.G("FirmaTelefon") + "  \u00B7  " + _e.G("FirmaEmail");
                string l2 = "IBAN: " + _e.G("BankIBAN") + "  \u00B7  " + _e.G("BankName") +
                            "  \u00B7  BIC: " + _e.G("BankBIC");
                string l3 = "USt-IdNr: " + _e.G("FirmaUstId") + "  \u00B7  " +
                            _e.G("FirmaRegistergericht") + "  \u00B7  " + _e.G("FirmaHandelsregister");

                float fy = 56f;
                foreach (var line in new[]{l1, l2, l3})
                {
                    float tw = _regular.GetWidth(line, 7f);
                    canvas.BeginText()
                        .SetFontAndSize(_regular, 7f).SetColor(fGrau, true)
                        .MoveText((w - tw) / 2f, fy)
                        .ShowText(line).EndText();
                    fy -= 10f;
                }

                canvas.Release();
            }
        }

        // ── ZUGFeRD Embedding ─────────────────────────────────────────

        public async Task<byte[]> RechnungZugferdAsync(Rechnung r, string zugferdXml)
        {
            var pdfBytes = await RechnungPdfAsync(r);
            var xmlBytes = System.Text.Encoding.UTF8.GetBytes(zugferdXml);
            using var inMs  = new MemoryStream(pdfBytes);
            using var outMs = new MemoryStream();
            var reader  = new PdfReader(inMs);
            var writer2 = new PdfWriter(outMs);
            using var pd = new PdfDocument(reader, writer2);
            var fs = iText.Kernel.Pdf.Filespec.PdfFileSpec.CreateEmbeddedFileSpec(
                pd, xmlBytes, "ZUGFeRD/Factur-X 2.3 EN 16931", "factur-x.xml",
                new iText.Kernel.Pdf.PdfName("text#2Fxml"), null,
                new iText.Kernel.Pdf.PdfName("Alternative"));
            pd.AddFileAttachment("factur-x.xml", fs);
            var af = new iText.Kernel.Pdf.PdfArray();
            af.Add(fs.GetPdfObject());
            pd.GetCatalog().Put(new iText.Kernel.Pdf.PdfName("AF"), af);
            var xmp = new iText.Kernel.Pdf.PdfStream(
                System.Text.Encoding.UTF8.GetBytes(
                    BuildZugferdXmp(r.Rechnungsnummer, r.Rechnungsdatum)));
            xmp.Put(iText.Kernel.Pdf.PdfName.Type,    new iText.Kernel.Pdf.PdfName("Metadata"));
            xmp.Put(iText.Kernel.Pdf.PdfName.Subtype, new iText.Kernel.Pdf.PdfName("XML"));
            pd.GetCatalog().Put(iText.Kernel.Pdf.PdfName.Metadata, xmp);
            pd.Close();
            return outMs.ToArray();
        }

        private static string BuildZugferdXmp(string nr, DateTime d) =>
            "<?xpacket begin=\"\xef\xbb\xbf\" id=\"W5M0MpCehiHzreSzNTczkc9d\"?>\n" +
            "<x:xmpmeta xmlns:x=\"adobe:ns:meta/\">\n" +
            "  <rdf:RDF xmlns:rdf=\"http://www.w3.org/1999/02/22-rdf-syntax-ns#\">\n" +
            "    <rdf:Description rdf:about=\"\"\n" +
            "        xmlns:fx=\"urn:factur-x:pdfa:CrossIndustryDocument:invoice:1p0#\">\n" +
            "      <fx:DocumentType>INVOICE</fx:DocumentType>\n" +
            "      <fx:DocumentFileName>factur-x.xml</fx:DocumentFileName>\n" +
            "      <fx:Version>1.0</fx:Version>\n" +
            "      <fx:ConformanceLevel>EN 16931</fx:ConformanceLevel>\n" +
            "    </rdf:Description>\n" +
            "    <rdf:Description rdf:about=\"\"\n" +
            "        xmlns:pdfaid=\"http://www.aiim.org/pdfa/ns/id/\">\n" +
            "      <pdfaid:part>3</pdfaid:part>\n" +
            "      <pdfaid:conformance>B</pdfaid:conformance>\n" +
            "    </rdf:Description>\n" +
            "  </rdf:RDF>\n" +
            "</x:xmpmeta>\n" +
            "<?xpacket end=\"w\"?>";

        private byte[]? LoadLogo()
        {
            try
            {
                var path = System.IO.Path.Combine(_env.WebRootPath, "images", "logo_gmbh.jpg");
                return File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            catch { return null; }
        }
    }

    internal static class DictExt
    {
        public static string G(this Dictionary<string,string> d, string key, string fallback = "")
            => d.TryGetValue(key, out var v) ? v ?? fallback : fallback;
    }
}
