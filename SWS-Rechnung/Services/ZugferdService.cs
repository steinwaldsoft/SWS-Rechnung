using System.Globalization;
using System.Text;
using System.Xml;
using SWSRechnung.Models;

namespace SWSRechnung.Services
{
    /// <summary>
    /// Erzeugt ein ZUGFeRD 2.3 / Factur-X EN 16931 konformes XML
    /// sowie ein XRechnung 3.x XML (CII-Syntax, gleiche Basis).
    /// </summary>
    public class ZugferdService
    {
        private readonly EinstellungenService _einst;
        private static readonly CultureInfo IC = CultureInfo.InvariantCulture;

        public ZugferdService(EinstellungenService einst) => _einst = einst;

        // ── Öffentliche API ───────────────────────────────────────

        /// <summary>Reines XRechnung-XML (CII) als byte[].</summary>
        public async Task<byte[]> XRechnungXmlAsync(Rechnung r)
        {
            var e = await _einst.GetAllAsync();
            var xml = BuildCiiXml(r, e);
            return Encoding.UTF8.GetBytes(xml);
        }

        /// <summary>ZUGFeRD-XML (identisch mit XRechnung CII EN 16931).</summary>
        public async Task<string> ZugferdXmlStringAsync(Rechnung r)
        {
            var e = await _einst.GetAllAsync();
            return BuildCiiXml(r, e);
        }

        // ── XML-Erzeugung ─────────────────────────────────────────

        private string BuildCiiXml(Rechnung r, Dictionary<string, string> e)
        {
            var sb = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                Indent = true,
                IndentChars = "  ",
                Encoding = new UTF8Encoding(false),
                OmitXmlDeclaration = false
            };

            using var sw = new StringWriter(sb);
            using var w  = XmlWriter.Create(sw, settings);

            // ── Namespaces ─────────────────────────────────────────
            w.WriteStartDocument();
            w.WriteStartElement("rsm", "CrossIndustryInvoice",
                "urn:un:unece:uncefact:data:standard:CrossIndustryInvoice:100");
            w.WriteAttributeString("xmlns", "qdt", null,
                "urn:un:unece:uncefact:data:standard:QualifiedDataType:100");
            w.WriteAttributeString("xmlns", "ram", null,
                "urn:un:unece:uncefact:data:standard:ReusableAggregateBusinessInformationEntity:100");
            w.WriteAttributeString("xmlns", "xs",  null,
                "http://www.w3.org/2001/XMLSchema");
            w.WriteAttributeString("xmlns", "udt", null,
                "urn:un:unece:uncefact:data:standard:UnqualifiedDataType:100");

            // ── ExchangedDocumentContext ───────────────────────────
            w.WriteStartElement("rsm", "ExchangedDocumentContext", null);
              w.WriteStartElement("ram", "GuidelineSpecifiedDocumentContextParameter", null);
                // EN 16931 Profil (kompatibel mit XRechnung & ZUGFeRD)
                w.WriteElementString("ram", "ID", null,
                    "urn:cen.eu:en16931:2017#compliant#urn:xoev-de:kosit:standard:xrechnung_3.0");
              w.WriteEndElement();
            w.WriteEndElement();

            // ── ExchangedDocument ─────────────────────────────────
            w.WriteStartElement("rsm", "ExchangedDocument", null);
              w.WriteElementString("ram", "ID",   null, r.Rechnungsnummer);
              // TypeCode 380 = Handelsrechnung
              w.WriteElementString("ram", "TypeCode", null, "380");
              w.WriteStartElement("ram", "IssueDateTime");
                w.WriteStartElement("udt", "DateTimeString");
                  w.WriteAttributeString("format", "102");
                  w.WriteString(r.Rechnungsdatum.ToString("yyyyMMdd"));
                w.WriteEndElement();
              w.WriteEndElement();
            w.WriteEndElement();

            // ── SupplyChainTradeTransaction ───────────────────────
            w.WriteStartElement("rsm", "SupplyChainTradeTransaction", null);

            // Positionen
            int posNr = 0;
            foreach (var p in r.Positionen.OrderBy(x => x.Position))
            {
                posNr++;
                w.WriteStartElement("ram", "IncludedSupplyChainTradeLineItem", null);

                  w.WriteStartElement("ram", "AssociatedDocumentLineDocument", null);
                    w.WriteElementString("ram", "LineID", null, posNr.ToString());
                  w.WriteEndElement();

                  w.WriteStartElement("ram", "SpecifiedTradeProduct", null);
                    w.WriteElementString("ram", "Name", null, p.Bezeichnung);
                    if (!string.IsNullOrEmpty(p.Beschreibung))
                        w.WriteElementString("ram", "Description", null, p.Beschreibung);
                  w.WriteEndElement();

                  w.WriteStartElement("ram", "SpecifiedLineTradeAgreement", null);
                    w.WriteStartElement("ram", "NetPriceProductTradePrice", null);
                      WriteAmount(w, "ram", "ChargeAmount", p.Einzelpreis);
                    w.WriteEndElement();
                  w.WriteEndElement();

                  w.WriteStartElement("ram", "SpecifiedLineTradeDelivery", null);
                    w.WriteStartElement("ram", "BilledQuantity");
                      w.WriteAttributeString("unitCode", MapEinheit(p.Einheit));
                      w.WriteString(p.Menge.ToString("F4", IC));
                    w.WriteEndElement();
                  w.WriteEndElement();

                  w.WriteStartElement("ram", "SpecifiedLineTradeSettlement", null);
                    w.WriteStartElement("ram", "ApplicableTradeTax", null);
                      w.WriteElementString("ram", "TypeCode",     null, "VAT");
                      w.WriteElementString("ram", "CategoryCode", null, "S");
                      WritePercent(w, "ram", "RateApplicablePercent", r.MwStSatz);
                    w.WriteEndElement();
                    w.WriteStartElement("ram", "SpecifiedTradeSettlementLineMonetarySummation", null);
                      WriteAmount(w, "ram", "LineTotalAmount", p.Gesamtpreis);
                    w.WriteEndElement();
                  w.WriteEndElement();

                w.WriteEndElement(); // IncludedSupplyChainTradeLineItem
            }

            // ── HeaderTradeAgreement ──────────────────────────────
            w.WriteStartElement("ram", "ApplicableHeaderTradeAgreement", null);

              // Verkäufer (Aussteller)
              w.WriteStartElement("ram", "SellerTradeParty", null);
                w.WriteElementString("ram", "Name", null, e.G("FirmaName"));
                w.WriteStartElement("ram", "PostalTradeAddress", null);
                  w.WriteElementString("ram", "PostcodeCode",    null, e.G("FirmaPLZ"));
                  w.WriteElementString("ram", "LineOne",         null, e.G("FirmaStrasse"));
                  w.WriteElementString("ram", "CityName",        null, e.G("FirmaOrt"));
                  w.WriteElementString("ram", "CountryID",       null, "DE");
                w.WriteEndElement();
                if (!string.IsNullOrEmpty(e.G("FirmaEmail")))
                {
                    w.WriteStartElement("ram", "URIUniversalCommunication", null);
                      w.WriteStartElement("ram", "URIID");
                        w.WriteAttributeString("schemeID", "EM");
                        w.WriteString(e.G("FirmaEmail"));
                      w.WriteEndElement();
                    w.WriteEndElement();
                }
                w.WriteStartElement("ram", "SpecifiedTaxRegistration", null);
                  w.WriteStartElement("ram", "ID");
                    w.WriteAttributeString("schemeID", "VA");
                    w.WriteString(e.G("FirmaUstId"));
                  w.WriteEndElement();
                w.WriteEndElement();
              w.WriteEndElement(); // SellerTradeParty

              // Käufer (Empfänger)
              w.WriteStartElement("ram", "BuyerTradeParty", null);
                w.WriteElementString("ram", "Name", null, r.Kunde?.Firmenname ?? "");
                if (r.Kunde != null)
                {
                    w.WriteStartElement("ram", "PostalTradeAddress", null);
                      w.WriteElementString("ram", "PostcodeCode", null, r.Kunde.PLZ ?? "");
                      w.WriteElementString("ram", "LineOne",      null, r.Kunde.Strasse ?? "");
                      w.WriteElementString("ram", "CityName",     null, r.Kunde.Ort ?? "");
                      w.WriteElementString("ram", "CountryID",    null, "DE");
                    w.WriteEndElement();
                    if (!string.IsNullOrEmpty(r.Kunde.Email))
                    {
                        w.WriteStartElement("ram", "URIUniversalCommunication", null);
                          w.WriteStartElement("ram", "URIID");
                            w.WriteAttributeString("schemeID", "EM");
                            w.WriteString(r.Kunde.Email);
                          w.WriteEndElement();
                        w.WriteEndElement();
                    }
                    if (!string.IsNullOrEmpty(r.Kunde.UstIdNr))
                    {
                        w.WriteStartElement("ram", "SpecifiedTaxRegistration", null);
                          w.WriteStartElement("ram", "ID");
                            w.WriteAttributeString("schemeID", "VA");
                            w.WriteString(r.Kunde.UstIdNr);
                          w.WriteEndElement();
                        w.WriteEndElement();
                    }
                }
              w.WriteEndElement(); // BuyerTradeParty

              // Bestellreferenz (optional, aber empfohlen)
              if (!string.IsNullOrEmpty(r.Betreff))
              {
                  w.WriteStartElement("ram", "BuyerOrderReferencedDocument", null);
                    w.WriteElementString("ram", "IssuerAssignedID", null, r.Betreff);
                  w.WriteEndElement();
              }

            w.WriteEndElement(); // ApplicableHeaderTradeAgreement

            // ── HeaderTradeDelivery ───────────────────────────────
            w.WriteStartElement("ram", "ApplicableHeaderTradeDelivery", null);
              if (r.LeistungBis.HasValue)
              {
                  w.WriteStartElement("ram", "ActualDeliverySupplyChainEvent", null);
                    w.WriteStartElement("ram", "OccurrenceDateTime");
                      w.WriteStartElement("udt", "DateTimeString");
                        w.WriteAttributeString("format", "102");
                        w.WriteString(r.LeistungBis.Value.ToString("yyyyMMdd"));
                      w.WriteEndElement();
                    w.WriteEndElement();
                  w.WriteEndElement();
              }
            w.WriteEndElement(); // ApplicableHeaderTradeDelivery

            // ── HeaderTradeSettlement ─────────────────────────────
            w.WriteStartElement("ram", "ApplicableHeaderTradeSettlement", null);

              w.WriteElementString("ram", "InvoiceCurrencyCode", null, "EUR");

              // Zahlungsmittel (SEPA-Überweisung)
              w.WriteStartElement("ram", "SpecifiedTradeSettlementPaymentMeans", null);
                w.WriteElementString("ram", "TypeCode", null, "58"); // 58 = SEPA
                w.WriteStartElement("ram", "PayeePartyCreditorFinancialAccount", null);
                  w.WriteElementString("ram", "IBANID", null,
                      e.G("BankIBAN").Replace(" ", ""));
                w.WriteEndElement();
                if (!string.IsNullOrEmpty(e.G("BankBIC")))
                {
                    w.WriteStartElement("ram", "PayeeSpecifiedCreditorFinancialInstitution", null);
                      w.WriteElementString("ram", "BICID", null, e.G("BankBIC"));
                    w.WriteEndElement();
                }
              w.WriteEndElement();

              // Steuer
              w.WriteStartElement("ram", "ApplicableTradeTax", null);
                WriteAmount(w, "ram", "CalculatedAmount",     r.MwStBetrag);
                w.WriteElementString("ram", "TypeCode",       null, "VAT");
                WriteAmount(w, "ram", "BasisAmount",          r.Nettobetrag);
                w.WriteElementString("ram", "CategoryCode",   null, "S");
                WritePercent(w, "ram", "RateApplicablePercent", r.MwStSatz);
              w.WriteEndElement();

              // Leistungszeitraum
              if (r.LeistungVon.HasValue || r.LeistungBis.HasValue)
              {
                  w.WriteStartElement("ram", "BillingSpecifiedPeriod", null);
                    if (r.LeistungVon.HasValue)
                    {
                        w.WriteStartElement("ram", "StartDateTime");
                          w.WriteStartElement("udt", "DateTimeString");
                            w.WriteAttributeString("format", "102");
                            w.WriteString(r.LeistungVon.Value.ToString("yyyyMMdd"));
                          w.WriteEndElement();
                        w.WriteEndElement();
                    }
                    if (r.LeistungBis.HasValue)
                    {
                        w.WriteStartElement("ram", "EndDateTime");
                          w.WriteStartElement("udt", "DateTimeString");
                            w.WriteAttributeString("format", "102");
                            w.WriteString(r.LeistungBis.Value.ToString("yyyyMMdd"));
                          w.WriteEndElement();
                        w.WriteEndElement();
                    }
                  w.WriteEndElement();
              }

              // Zahlungsziel
              w.WriteStartElement("ram", "SpecifiedTradePaymentTerms", null);
                if (r.FaelligAm.HasValue)
                {
                    w.WriteStartElement("ram", "DueDateDateTime");
                      w.WriteStartElement("udt", "DateTimeString");
                        w.WriteAttributeString("format", "102");
                        w.WriteString(r.FaelligAm.Value.ToString("yyyyMMdd"));
                      w.WriteEndElement();
                    w.WriteEndElement();
                }
              w.WriteEndElement();

              // Summen
              w.WriteStartElement("ram", "SpecifiedTradeSettlementHeaderMonetarySummation", null);
                WriteAmount(w, "ram", "LineTotalAmount",      r.Nettobetrag);
                WriteAmount(w, "ram", "TaxBasisTotalAmount",  r.Nettobetrag);
                WriteAmount(w, "ram", "TaxTotalAmount",       r.MwStBetrag,  currencyAttr: true);
                WriteAmount(w, "ram", "GrandTotalAmount",     r.Bruttobetrag);
                WriteAmount(w, "ram", "DuePayableAmount",     r.Bruttobetrag);
              w.WriteEndElement();

            w.WriteEndElement(); // ApplicableHeaderTradeSettlement

            w.WriteEndElement(); // SupplyChainTradeTransaction
            w.WriteEndElement(); // CrossIndustryInvoice
            w.WriteEndDocument();
            w.Flush();

            return sb.ToString();
        }

        // ── Hilfsmethoden ─────────────────────────────────────────

        private static void WriteAmount(XmlWriter w, string ns, string element,
            decimal value, bool currencyAttr = false)
        {
            w.WriteStartElement(ns, element, null);
            if (currencyAttr)
                w.WriteAttributeString("currencyID", "EUR");
            w.WriteString(value.ToString("F2", IC));
            w.WriteEndElement();
        }

        private static void WritePercent(XmlWriter w, string ns, string element, decimal value)
        {
            w.WriteStartElement(ns, element, null);
            w.WriteString(value.ToString("F2", IC));
            w.WriteEndElement();
        }

        /// <summary>Mappt deutsche Einheiten auf UN/ECE-Codes (Rec 20).</summary>
        private static string MapEinheit(string einheit) => einheit.Trim().ToLower() switch
        {
            "std."  or "std" or "h"   or "stunde"  or "stunden"  => "HUR", // Hour
            "tag"   or "tage"  or "d"                             => "DAY",
            "monat" or "monate"or "mon"                           => "MON",
            "stk"   or "stück" or "stk." or "stk"                => "C62", // piece
            "km"                                                  => "KMT",
            "kg"                                                  => "KGM",
            "l"     or "liter"                                    => "LTR",
            "m"     or "meter"                                    => "MTR",
            "m²"    or "m2"                                       => "MTK",
            "pauschal" or "psch" or "psch."                       => "LS",  // lump sum
            _                                                     => "C62"  // Fallback: piece
        };
    }
}
