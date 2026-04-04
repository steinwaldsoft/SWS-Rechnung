using System.Globalization;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using SWSRechnung.Models;
using A   = DocumentFormat.OpenXml.Drawing;
using DW  = DocumentFormat.OpenXml.Drawing.Wordprocessing;
using PIC = DocumentFormat.OpenXml.Drawing.Pictures;
using WP  = DocumentFormat.OpenXml.Wordprocessing;

namespace SWSRechnung.Services
{
    /// <summary>
    /// Erzeugt Rechnungen und Angebote als .docx – Layout wie Beispielrechnung:
    /// Logo zentriert in Kopfzeile, Adress-/Meta-Block, Positionen-Tabelle,
    /// Summenblock, Fußzeile mit Firmeninfos + "Seite X von Y".
    /// </summary>
    public class DocxService
    {
        private readonly EinstellungenService _einst;
        private readonly IWebHostEnvironment  _env;
        private static readonly CultureInfo DE = new("de-DE");

        // Markenfarben (ohne #)
        private const string BLAU    = "0070C0";
        private const string GRUEN   = "B0C000";
        private const string GRAU    = "808080";
        private const string HELLBLAU= "EBF4FB";
        private const string DUNKEL  = "1E1E1E";

        // A4 in Twips (1 cm ≈ 567 twips)
        private const int PAGE_W = 11906;
        private const int PAGE_H = 16838;
        private const int MARG_L = 1701;   // 3,0 cm
        private const int MARG_R = 1134;   // 2,0 cm
        private const int MARG_T = 567;    // 1,0 cm (Kopfzeile kompakter)
        private const int MARG_B = 1180;   // 2,1 cm (knapp über Fußzeile)
        private const int BODY_W = PAGE_W - MARG_L - MARG_R; // 9071 twips

        // Logo-Größe in EMU (914400 = 1 Zoll = 2,54 cm)
        // Logo ist 1545 x 386 px; Ziel: 6 cm Breite
        private const long LOGO_W = 2160000L; // 6,0 cm
        private const long LOGO_H =  539650L; // 1,5 cm

        public DocxService(EinstellungenService einst, IWebHostEnvironment env)
        {
            _einst = einst;
            _env   = env;
        }

        // ── Öffentliche API ───────────────────────────────────────────

        public async Task<byte[]> RechnungDocxAsync(Rechnung r)
        {
            var e = await _einst.GetAllAsync();
            return Build(e,
                typ:         "RECHNUNG",
                nummer:      r.Rechnungsnummer,
                datum:       r.Rechnungsdatum,
                extraLabel:  "Zahlbar bis",
                extraValue:  r.FaelligAm?.ToString("dd.MM.yyyy") ?? "–",
                extraBold:   true,
                extraBlau:   true,
                kundennr:    r.Kunde?.Kundennummer ?? "",
                kunde:       r.Kunde,
                betreff:     r.Betreff,
                einleitung:  r.Einleitung,
                schlusstext: r.Schlusstext,
                mwstSatz:    r.MwStSatz,
                netto:       r.Nettobetrag,
                mwst:        r.MwStBetrag,
                brutto:      r.Bruttobetrag,
                positionen:  r.Positionen.OrderBy(p => p.Position)
                              .Select(p => (p.Position, p.Bezeichnung,
                                            p.Beschreibung, p.Menge, p.Einheit,
                                            p.Einzelpreis, p.Rabatt, p.Gesamtpreis))
                              .ToList());
        }

        public async Task<byte[]> AngebotDocxAsync(Angebot a)
        {
            var e = await _einst.GetAllAsync();
            return Build(e,
                typ:         "ANGEBOT",
                nummer:      a.Angebotsnummer,
                datum:       a.Angebotsdatum,
                extraLabel:  "Gültig bis",
                extraValue:  a.GueltigBis?.ToString("dd.MM.yyyy") ?? "–",
                extraBold:   false,
                extraBlau:   false,
                kundennr:    a.Kunde?.Kundennummer ?? "",
                kunde:       a.Kunde,
                betreff:     a.Betreff,
                einleitung:  a.Einleitung,
                schlusstext: a.Schlusstext,
                mwstSatz:    a.MwStSatz,
                netto:       a.Nettobetrag,
                mwst:        a.MwStBetrag,
                brutto:      a.Bruttobetrag,
                positionen:  a.Positionen.OrderBy(p => p.Position)
                              .Select(p => (p.Position, p.Bezeichnung,
                                            p.Beschreibung, p.Menge, p.Einheit,
                                            p.Einzelpreis, p.Rabatt, p.Gesamtpreis))
                              .ToList());
        }

        // ── Kern-Builder ──────────────────────────────────────────────

        private byte[] Build(
            Dictionary<string,string> e,
            string typ, string nummer, DateTime datum,
            string extraLabel, string extraValue, bool extraBold, bool extraBlau,
            string kundennr, Kunde? kunde,
            string? betreff, string? einleitung, string? schlusstext,
            decimal mwstSatz, decimal netto, decimal mwst, decimal brutto,
            List<(int Pos, string Bez, string? Beschr,
                  decimal Menge, string Einheit,
                  decimal EP, decimal Rabatt, decimal GP)> positionen)
        {
            using var ms = new MemoryStream();
            using (var wordDoc = WordprocessingDocument.Create(ms,
                       WordprocessingDocumentType.Document, true))
            {
                var main = wordDoc.AddMainDocumentPart();
                main.Document = new WP.Document(new WP.Body());
                var body = main.Document.Body!;

                // Styles
                var sp = main.AddNewPart<StyleDefinitionsPart>();
                sp.Styles = MakeStyles();
                sp.Styles.Save();

                // Header und Footer anlegen — IDs für SectionProperties
                byte[]? logo = LoadLogo();
                var hdrPart  = main.AddNewPart<HeaderPart>();
                FillHeader(hdrPart, logo, e);
                string hdrId = main.GetIdOfPart(hdrPart);
                var ftrPart  = main.AddNewPart<FooterPart>();
                FillFooter(ftrPart, e);
                string ftrId = main.GetIdOfPart(ftrPart);

                // ── Body ──────────────────────────────────────────────

                // Typ-Box (RECHNUNG / ANGEBOT) rechts oben
                body.AppendChild(MakeTypBox(typ, nummer));
                body.AppendChild(Sp(80));

                // Adresse | Metadaten
                body.AppendChild(MakeAdresseMeta(
                    kunde, datum, extraLabel, extraValue, extraBold, extraBlau,
                    kundennr, e));
                body.AppendChild(Sp(240));

                // Betreff
                if (!string.IsNullOrEmpty(betreff))
                    body.AppendChild(P(betreff, bold: true, sz: 24,
                        col: BLAU, after: 120));

                // Anrede + Einleitung
                body.AppendChild(P(MakeAnrede(kunde), sz: 22, after: 80));
                if (!string.IsNullOrEmpty(einleitung))
                    body.AppendChild(P(einleitung, sz: 22, after: 160));

                // Positionen
                body.AppendChild(MakePosTable(positionen));
                body.AppendChild(Sp(80));

                // Summen
                body.AppendChild(MakeSumTable(mwstSatz, netto, mwst, brutto));
                body.AppendChild(Sp(280));

                // Schlusstext
                if (!string.IsNullOrEmpty(schlusstext))
                    foreach (var line in schlusstext.Split('\n'))
                        body.AppendChild(P(line.TrimEnd('\r'), sz: 22, after: 60));

                body.AppendChild(Sp(160));

                // Section Properties (Seite + Header/Footer)
                body.AppendChild(new WP.SectionProperties(
                    new WP.PageSize     { Width = PAGE_W, Height = PAGE_H },
                    new WP.PageMargin   {
                        Top    = MARG_T, Bottom = MARG_B,
                        Left   = (uint)MARG_L, Right  = (uint)MARG_R,
                        Header = 400u,  Footer = 280u
                    },
                    new WP.HeaderReference {
                        Type = WP.HeaderFooterValues.Default, Id = hdrId },
                    new WP.FooterReference {
                        Type = WP.HeaderFooterValues.Default, Id = ftrId }
                ));

                main.Document.Save();
            }
            return ms.ToArray();
        }

        // ── Kopfzeile: Logo zentriert ─────────────────────────────────

        private static void FillHeader(HeaderPart part,
            byte[]? logo, Dictionary<string,string> e)
        {
            var header = new WP.Header();

            if (logo != null)
            {
                var imgPart = part.AddImagePart(ImagePartType.Jpeg);
                imgPart.FeedData(new MemoryStream(logo));
                string imgId = part.GetIdOfPart(imgPart);

                header.AppendChild(new WP.Paragraph(
                    new WP.ParagraphProperties(
                        new WP.Justification { Val = WP.JustificationValues.Center },
                        new WP.SpacingBetweenLines { Before = "0", After = "40" }
                    ),
                    InlineImageRun(imgId, LOGO_W, LOGO_H, "Logo steinwald.soft")
                ));
            }
            else
            {
                header.AppendChild(P(e.G("FirmaName"), bold: true, sz: 28,
                    col: BLAU, align: WP.JustificationValues.Center, after: 80));
            }

            // Kleiner Abstand nach Logo (keine Trennlinie)
            header.AppendChild(new WP.Paragraph(
                new WP.ParagraphProperties(
                    new WP.SpacingBetweenLines { Before = "0", After = "60" }
                )
            ));

            part.Header = header;
            part.Header.Save();
        }

        // ── Fußzeile: Seitenzahl + Firmendaten ────────────────────────

        private static void FillFooter(FooterPart part,
            Dictionary<string,string> e)
        {
            var footer = new WP.Footer();

            // 1. Grüne Trennlinie
            footer.AppendChild(new WP.Paragraph(
                new WP.ParagraphProperties(
                    new WP.SpacingBetweenLines { Before = "0", After = "0" },
                    new WP.ParagraphBorders(new WP.TopBorder {
                        Val   = WP.BorderValues.None
                    })
                )
            ));

            // 2. Seitenzahl rechtsbündig
            footer.AppendChild(PageNumPara());

            // 3. Firmeninfos zentriert in drei Zeilen
            string line1 = e.G("FirmaName") + "  ·  " +
                           e.G("FirmaStrasse") + "  ·  " +
                           e.G("FirmaPLZ") + " " + e.G("FirmaOrt") +
                           "  ·  Tel: " + e.G("FirmaTelefon") +
                           "  ·  " + e.G("FirmaEmail");
            string line2 = "IBAN: " + e.G("BankIBAN") +
                           "  ·  " + e.G("BankName") +
                           "  ·  BIC: " + e.G("BankBIC");
            string line3 = "USt-IdNr: " + e.G("FirmaUstId") +
                           "  ·  " + e.G("FirmaRegistergericht") +
                           "  ·  " + e.G("FirmaHandelsregister");

            foreach (var line in new[] { line1, line2, line3 })
                footer.AppendChild(FtrLine(line));

            part.Footer = footer;
            part.Footer.Save();
        }

        // Seitenzahlzeile mit PAGE / NUMPAGES Feldern
        private static WP.Paragraph PageNumPara()
        {
            var rPr = new WP.RunProperties(
                new WP.FontSize { Val = "13" },
                new WP.Color   { Val = GRAU });

            WP.Run FieldRun(string code) => new WP.Run((WP.RunProperties)rPr.CloneNode(true),
                new WP.FieldChar { FieldCharType = WP.FieldCharValues.Begin });
            WP.Run CodeRun(string code) => new WP.Run((WP.RunProperties)rPr.CloneNode(true),
                new WP.FieldCode(" " + code + " ")
                    { Space = SpaceProcessingModeValues.Preserve });
            WP.Run SepRun() => new WP.Run((WP.RunProperties)rPr.CloneNode(true),
                new WP.FieldChar { FieldCharType = WP.FieldCharValues.Separate });
            WP.Run EndRun() => new WP.Run((WP.RunProperties)rPr.CloneNode(true),
                new WP.FieldChar { FieldCharType = WP.FieldCharValues.End });
            WP.Run TxtRun(string t) => new WP.Run((WP.RunProperties)rPr.CloneNode(true),
                new WP.Text(t) { Space = SpaceProcessingModeValues.Preserve });

            return new WP.Paragraph(
                new WP.ParagraphProperties(
                    new WP.Justification { Val = WP.JustificationValues.Right },
                    new WP.SpacingBetweenLines { Before = "0", After = "0" }
                ),
                TxtRun("Seite "),
                FieldRun("PAGE"), CodeRun("PAGE"), SepRun(), EndRun(),
                TxtRun(" von "),
                FieldRun("NUMPAGES"), CodeRun("NUMPAGES"), SepRun(), EndRun()
            );
        }

        private static WP.Paragraph FtrLine(string text) =>
            new WP.Paragraph(
                new WP.ParagraphProperties(
                    new WP.Justification { Val = WP.JustificationValues.Center },
                    new WP.SpacingBetweenLines { Before = "0", After = "0" }
                ),
                new WP.Run(
                    new WP.RunProperties(
                        new WP.FontSize { Val = "12" },
                        new WP.Color   { Val = GRAU }
                    ),
                    new WP.Text(text) { Space = SpaceProcessingModeValues.Preserve }
                )
            );

        // ── Typ-Box oben rechts ───────────────────────────────────────

        private static WP.Table MakeTypBox(string typ, string nummer)
        {
            int boxW  = 2800;
            int leftW = BODY_W - boxW;
            var tbl   = NewTable(BODY_W, new[]{ leftW, boxW });
            NoBorders(tbl);

            var row = new WP.TableRow();

            // Leere linke Seite
            var lc = new WP.TableCell();
            CellW(lc, leftW);
            lc.AppendChild(new WP.Paragraph(
                new WP.ParagraphProperties(
                    new WP.SpacingBetweenLines { Before = "0", After = "0" })));
            row.AppendChild(lc);

            // Grüne Box rechts (Markenfarbe #B0C000)
            var rc = new WP.TableCell();
            CellW(rc, boxW);
            CellBg(rc, GRUEN);
            CellPad(rc, 80, 120);
            rc.AppendChild(P(typ, bold: true, sz: 32, col: "FFFFFF",
                align: WP.JustificationValues.Right, after: 20, before: 60));
            rc.AppendChild(P(nummer, sz: 18, col: "F0F5CC",
                align: WP.JustificationValues.Right, after: 60));
            row.AppendChild(rc);

            tbl.AppendChild(row);
            return tbl;
        }

        // ── Adresse + Metadaten nebeneinander ────────────────────────

        private static WP.Table MakeAdresseMeta(
            Kunde? kunde, DateTime datum,
            string extraLabel, string extraValue, bool extraBold, bool extraBlau,
            string kundennr, Dictionary<string,string> e)
        {
            int metaW = 2600;
            int addrW = BODY_W - metaW;
            var tbl   = NewTable(BODY_W, new[]{ addrW, metaW });
            NoBorders(tbl);

            var row = new WP.TableRow();

            // Linke Zelle: Absender-Kurzzeile + Empfänger
            var lc = new WP.TableCell();
            CellW(lc, addrW);
            CellPad(lc, 0, 0);

            // Absender (kursiv, grau, unterstrichen)
            string absender = e.G("FirmaName") + "  \u00B7  " +
                              e.G("FirmaStrasse") + "  \u00B7  " +
                              e.G("FirmaPLZ") + " " + e.G("FirmaOrt");
            lc.AppendChild(P(absender, sz: 15, col: GRAU,
                after: 80, underline: true));

            if (kunde != null)
            {
                lc.AppendChild(P(kunde.Firmenname, bold: true, sz: 24,
                    col: DUNKEL, after: 40));
                if (!string.IsNullOrEmpty(kunde.Ansprechpartner))
                    lc.AppendChild(P(
                        ((kunde.Anrede ?? "") + " " + kunde.Ansprechpartner).Trim(),
                        sz: 22, after: 20));
                if (!string.IsNullOrEmpty(kunde.Strasse))
                    lc.AppendChild(P(kunde.Strasse, sz: 22, after: 20));
                lc.AppendChild(P(
                    (kunde.PLZ ?? "") + " " + (kunde.Ort ?? ""),
                    sz: 22, after: 20));
            }
            row.AppendChild(lc);

            // Rechte Zelle: Datum / Kundennummer / Fälligkeit
            var rc = new WP.TableCell();
            CellW(rc, metaW);
            CellPad(rc, 0, 0);

            void MetaBlock(string label, string val,
                           bool bold = false, string col = DUNKEL)
            {
                rc.AppendChild(P(label, sz: 18, col: GRAU,
                    align: WP.JustificationValues.Right, after: 0));
                rc.AppendChild(P(val, bold: bold, sz: 22, col: col,
                    align: WP.JustificationValues.Right, after: 80));
            }

            MetaBlock("Datum", datum.ToString("dd.MM.yyyy"));
            if (!string.IsNullOrEmpty(kundennr))
                MetaBlock("Kundennummer", kundennr);
            MetaBlock(extraLabel, extraValue,
                      bold:  extraBold,
                      col:   extraBlau ? BLAU : DUNKEL);

            row.AppendChild(rc);
            tbl.AppendChild(row);
            return tbl;
        }

        // ── Positions-Tabelle ─────────────────────────────────────────

        private static WP.Table MakePosTable(
            List<(int Pos, string Bez, string? Beschr,
                  decimal Menge, string Einheit,
                  decimal EP, decimal Rabatt, decimal GP)> pos)
        {
            // Spaltenbreiten: Pos | Bezeichnung | Menge | Einh | EP | Rbt% | Gesamt
            int[] cw = { 500, 3101, 800, 700, 1400, 600, 1970 };
            var tbl  = NewTable(BODY_W, cw);
            // Alle Tabellen-Rahmen entfernen – verhindert weiße Striche im Header
            NoBorders(tbl);

            // Header-Zeile
            (string t, WP.JustificationValues a)[] hdr =
            {
                ("Pos.",        WP.JustificationValues.Center),
                ("Bezeichnung", WP.JustificationValues.Left),
                ("Menge",       WP.JustificationValues.Right),
                ("Einh.",       WP.JustificationValues.Center),
                ("Einzelpr.",  WP.JustificationValues.Right),
                ("Rbt%",        WP.JustificationValues.Right),
                ("Gesamt",      WP.JustificationValues.Right),
            };
            var hRow = new WP.TableRow();
            for (int i = 0; i < hdr.Length; i++)
            {
                var hc = new WP.TableCell();
                CellW(hc, cw[i]);
                CellBg(hc, BLAU);
                CellPad(hc, 20, 50);
                // NoWrap + alle Zellrahmen None (verhindert weiße Striche)
                var hcProp = hc.GetFirstChild<WP.TableCellProperties>()
                             ?? hc.AppendChild(new WP.TableCellProperties());
                hcProp.AppendChild(new WP.NoWrap());
                hcProp.AppendChild(new WP.TableCellBorders(
                    new WP.TopBorder    { Val=WP.BorderValues.None },
                    new WP.BottomBorder { Val=WP.BorderValues.None },
                    new WP.LeftBorder   { Val=WP.BorderValues.None },
                    new WP.RightBorder  { Val=WP.BorderValues.None }
                ));
                hc.AppendChild(P(hdr[i].t, bold: true, sz: 18,
                    col: "FFFFFF", align: hdr[i].a, after: 0, before: 10));
                hRow.AppendChild(hc);
            }
            tbl.AppendChild(hRow);

            // Datenzeilen
            bool alt = false;
            foreach (var p in pos)
            {
                string fill = alt ? HELLBLAU : "FFFFFF";
                var dRow    = new WP.TableRow();

                dRow.AppendChild(DC(cw[0], p.Pos.ToString(), fill,
                    WP.JustificationValues.Center));

                // Bezeichnung + Beschreibung
                var bc = new WP.TableCell();
                CellW(bc, cw[1]); CellBg(bc, fill);
                CellPad(bc, 60, 50); CellTopBorder(bc);
                bc.AppendChild(P(p.Bez, bold: true, sz: 20,
                    after: 0, before: 40));
                if (!string.IsNullOrEmpty(p.Beschr))
                    bc.AppendChild(P(p.Beschr, sz: 18, col: GRAU, after: 0));
                dRow.AppendChild(bc);

                dRow.AppendChild(DC(cw[2], p.Menge.ToString("N2", DE), fill,
                    WP.JustificationValues.Right));
                dRow.AppendChild(DC(cw[3], p.Einheit, fill,
                    WP.JustificationValues.Center));
                dRow.AppendChild(DC(cw[4],
                    p.EP.ToString("N2", DE) + "\u00A0\u20AC", fill,
                    WP.JustificationValues.Right));
                dRow.AppendChild(DC(cw[5],
                    p.Rabatt > 0
                        ? p.Rabatt.ToString("N1", DE) + "%"
                        : "\u2013",
                    fill, WP.JustificationValues.Right, col: GRAU));
                dRow.AppendChild(DC(cw[6],
                    p.GP.ToString("N2", DE) + "\u00A0\u20AC", fill,
                    WP.JustificationValues.Right, bold: true));

                tbl.AppendChild(dRow);
                alt = !alt;
            }
            return tbl;
        }

        // ── Summentabelle ─────────────────────────────────────────────

        private static WP.Table MakeSumTable(
            decimal mwstSatz, decimal netto, decimal mwst, decimal brutto)
        {
            int valW  = 2100;
            int lblW  = BODY_W - valW;
            var tbl   = NewTable(BODY_W, new[]{ lblW, valW });
            NoBorders(tbl);

            void Row(string label, string value,
                     bool bold = false, bool totals = false)
            {
                string fill = totals ? BLAU : "FFFFFF";
                string fc   = totals ? "FFFFFF" : DUNKEL;
                int    sz   = totals ? 22 : 21;
                int    pad  = totals ? 30 : 40;

                var row = new WP.TableRow();
                foreach (var (w, txt, al) in new[]
                {
                    (lblW, label, WP.JustificationValues.Right),
                    (valW, value, WP.JustificationValues.Right)
                })
                {
                    var c = new WP.TableCell();
                    CellW(c, w); CellBg(c, fill); CellPad(c, pad, 100);
                    c.AppendChild(P(txt, bold: bold, sz: sz, col: fc,
                        align: al, after: pad/2, before: pad/2));
                    row.AppendChild(c);
                }
                tbl.AppendChild(row);
            }

            Row("Nettobetrag:",          Eur(netto));
            Row($"MwSt. ({mwstSatz:N0} %):", Eur(mwst));
            Row("Gesamtbetrag:",         Eur(brutto), bold: true, totals: true);
            return tbl;
        }

        // ── Bild-Hilfsmethode ─────────────────────────────────────────

        private static WP.Run InlineImageRun(
            string relId, long cx, long cy, string name)
        {
            uint nextId = 1U;
            var inline = new DW.Inline(
                new DW.Extent            { Cx = cx, Cy = cy },
                new DW.EffectExtent      { LeftEdge=0, TopEdge=0, RightEdge=0, BottomEdge=0 },
                new DW.DocProperties     { Id = nextId, Name = name },
                new DW.NonVisualGraphicFrameDrawingProperties(
                    new A.GraphicFrameLocks { NoChangeAspect = true }),
                new A.Graphic(
                    new A.GraphicData(
                        new PIC.Picture(
                            new PIC.NonVisualPictureProperties(
                                new PIC.NonVisualDrawingProperties
                                    { Id = 0U, Name = name },
                                new PIC.NonVisualPictureDrawingProperties()),
                            new PIC.BlipFill(
                                new A.Blip {
                                    Embed = relId,
                                    CompressionState = A.BlipCompressionValues.Print
                                },
                                new A.Stretch(new A.FillRectangle())),
                            new PIC.ShapeProperties(
                                new A.Transform2D(
                                    new A.Offset   { X=0L, Y=0L },
                                    new A.Extents  { Cx=cx, Cy=cy }),
                                new A.PresetGeometry(new A.AdjustValueList())
                                    { Preset = A.ShapeTypeValues.Rectangle })
                        )
                    ) { Uri = "http://schemas.openxmlformats.org/drawingml/2006/picture" }
                )
            ) { DistanceFromTop=0U, DistanceFromBottom=0U,
                DistanceFromLeft=0U, DistanceFromRight=0U };

            return new WP.Run(
                new WP.RunProperties(new WP.NoProof()),
                new WP.Drawing(inline));
        }

        // ── Styles ────────────────────────────────────────────────────

        private static WP.Styles MakeStyles() => new WP.Styles(
            new WP.DocDefaults(
                new WP.RunPropertiesDefault(
                    new WP.RunPropertiesBaseStyle(
                        new WP.RunFonts { Ascii="Calibri", HighAnsi="Calibri" },
                        new WP.FontSize { Val="22" }
                    )
                ),
                new WP.ParagraphPropertiesDefault(
                    new WP.ParagraphPropertiesBaseStyle(
                        new WP.SpacingBetweenLines { Before="0", After="0" }
                    )
                )
            )
        );

        // ── Paragraph ─────────────────────────────────────────────────

        private static WP.Paragraph P(
            string text,
            bool bold      = false,
            int  sz        = 22,
            string col     = DUNKEL,
            WP.JustificationValues align = default,
            int  after     = 80,
            int  before    = 0,
            bool underline = false)
        {
            var rPr = new WP.RunProperties(
                new WP.RunFonts { Ascii="Calibri", HighAnsi="Calibri" },
                new WP.FontSize { Val = sz.ToString() },
                new WP.Color   { Val = col }
            );
            if (bold)      rPr.AppendChild(new WP.Bold());
            if (underline) rPr.AppendChild(
                new WP.Underline { Val = WP.UnderlineValues.Single });

            return new WP.Paragraph(
                new WP.ParagraphProperties(
                    new WP.Justification       { Val = align },
                    new WP.SpacingBetweenLines {
                        Before = before.ToString(),
                        After  = after.ToString()
                    }
                ),
                new WP.Run(rPr,
                    new WP.Text(text) { Space = SpaceProcessingModeValues.Preserve })
            );
        }

        private static WP.Paragraph Sp(int twips) =>
            new WP.Paragraph(new WP.ParagraphProperties(
                new WP.SpacingBetweenLines { Before="0", After=twips.ToString() }));

        // ── Tabellen-Helpers ──────────────────────────────────────────

        private static WP.Table NewTable(int totalW, int[] colW)
        {
            var tbl = new WP.Table();
            tbl.AppendChild(new WP.TableProperties(
                new WP.TableWidth  { Width=totalW.ToString(),
                    Type=WP.TableWidthUnitValues.Dxa },
                new WP.TableLayout { Type=WP.TableLayoutValues.Fixed }
            ));
            var grid = new WP.TableGrid();
            foreach (var w in colW)
                grid.AppendChild(new WP.GridColumn { Width=w.ToString() });
            tbl.AppendChild(grid);
            return tbl;
        }

        private static void NoBorders(WP.Table tbl)
        {
            var tp = tbl.GetFirstChild<WP.TableProperties>()!;
            tp.AppendChild(new WP.TableBorders(
                new WP.TopBorder              { Val=WP.BorderValues.None },
                new WP.BottomBorder           { Val=WP.BorderValues.None },
                new WP.LeftBorder             { Val=WP.BorderValues.None },
                new WP.RightBorder            { Val=WP.BorderValues.None },
                new WP.InsideHorizontalBorder { Val=WP.BorderValues.None },
                new WP.InsideVerticalBorder   { Val=WP.BorderValues.None }
            ));
        }

        private static void CellW(WP.TableCell c, int w)
        {
            var tcp = c.GetFirstChild<WP.TableCellProperties>()
                      ?? c.AppendChild(new WP.TableCellProperties());
            tcp.AppendChild(new WP.TableCellWidth
                { Width=w.ToString(), Type=WP.TableWidthUnitValues.Dxa });
        }

        private static void CellBg(WP.TableCell c, string fill)
        {
            var tcp = c.GetFirstChild<WP.TableCellProperties>()
                      ?? c.AppendChild(new WP.TableCellProperties());
            tcp.AppendChild(new WP.Shading
                { Val=WP.ShadingPatternValues.Clear, Fill=fill, Color="auto" });
        }

        private static void CellPad(WP.TableCell c, int tb, int lr)
        {
            var tcp = c.GetFirstChild<WP.TableCellProperties>()
                      ?? c.AppendChild(new WP.TableCellProperties());
            tcp.AppendChild(new WP.TableCellMargin(
                new WP.TopMargin    { Width=tb.ToString(), Type=WP.TableWidthUnitValues.Dxa },
                new WP.BottomMargin { Width=tb.ToString(), Type=WP.TableWidthUnitValues.Dxa },
                new WP.LeftMargin   { Width=lr.ToString(), Type=WP.TableWidthUnitValues.Dxa },
                new WP.RightMargin  { Width=lr.ToString(), Type=WP.TableWidthUnitValues.Dxa }
            ));
        }

        private static void CellTopBorder(WP.TableCell c)
        {
            var tcp = c.GetFirstChild<WP.TableCellProperties>()
                      ?? c.AppendChild(new WP.TableCellProperties());
            tcp.AppendChild(new WP.TableCellBorders(
                new WP.TopBorder    { Val=WP.BorderValues.Single,
                    Color="DDDDDD", Size=4, Space=0 },
                new WP.BottomBorder { Val=WP.BorderValues.None },
                new WP.LeftBorder   { Val=WP.BorderValues.None },
                new WP.RightBorder  { Val=WP.BorderValues.None }
            ));
        }

        // Standard-Datenzelle für Positions-Tabelle
        private static WP.TableCell DC(int w, string text, string fill,
            WP.JustificationValues al,
            bool bold = false, string col = DUNKEL)
        {
            var c = new WP.TableCell();
            CellW(c, w); CellBg(c, fill);
            CellPad(c, 60, 50); CellTopBorder(c);
            c.AppendChild(P(text, bold: bold, sz: 20, col: col,
                align: al, after: 0, before: 40));
            return c;
        }

        // ── Sonstige Helpers ──────────────────────────────────────────

        private static string MakeAnrede(Kunde? k)
        {
            if (k?.Ansprechpartner == null) return "Sehr geehrte Damen und Herren,";
            return k.Anrede switch {
                "Herr" => $"Sehr geehrter Herr {k.Ansprechpartner},",
                "Frau" => $"Sehr geehrte Frau {k.Ansprechpartner},",
                _      => $"Sehr geehrte/r {k.Ansprechpartner},"
            };
        }

        private static string Eur(decimal v) =>
            v.ToString("N2", DE) + "\u00A0\u20AC";

        private byte[]? LoadLogo()
        {
            try
            {
                var path = Path.Combine(_env.WebRootPath, "images", "logo_gmbh.jpg");
                return File.Exists(path) ? File.ReadAllBytes(path) : null;
            }
            catch { return null; }
        }
    }
}
