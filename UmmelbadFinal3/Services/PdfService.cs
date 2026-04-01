using System;
using System.Globalization;
using System.IO;
using System.Linq;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using UmmelbadFinal3.Models;

namespace UmmelbadFinal3.Services
{
    public class PdfService
    {
        private readonly string _baseDirectory;
        private readonly CultureInfo _culture = new("de-DE");

        public PdfService(string baseDirectory)
        {
            _baseDirectory = baseDirectory;
            QuestPDF.Settings.License = LicenseType.Community;
        }

        public string Export(Invoice invoice)
        {
            var outputPath = Path.Combine(_baseDirectory, $"Rechnung_{invoice.InvoiceNumber}.pdf");

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Content().Element(e => ComposeContent(e, invoice));
                });
            }).GeneratePdf(outputPath);

            return outputPath;
        }

        private void ComposeContent(IContainer container, Invoice invoice)
        {
            var servicePeriodStart = invoice.ServiceDate ?? invoice.InvoiceDate;
            var servicePeriodEnd = invoice.ServiceDate ?? invoice.InvoiceDate;
            var taxGroups = invoice.Items
                .GroupBy(i => i.TaxRate)
                .OrderBy(g => g.Key)
                .Select(g => new TaxGroup(
                    g.Key,
                    g.Sum(item => item.NetTotal),
                    g.Sum(item => item.TaxAmount)))
                .ToList();

            container.Column(col =>
            {
                col.Spacing(12);

                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Spacing(2);
                        left.Item().Text("Ihre Firma GmbH").SemiBold();
                        left.Item().Text("Musterstraße 1");
                        left.Item().Text("12345 Musterstadt");
                        left.Item().Text("Telefon: +49 123 456789");
                        left.Item().Text("E-Mail: info@ihrefirma.de");
                    });

                    row.ConstantItem(190).Height(70).AlignRight().AlignTop().Element(c =>
                    {
                        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.png");
                        if (File.Exists(logoPath))
                        {
                            c.Image(File.ReadAllBytes(logoPath), ImageScaling.FitWidth);
                        }
                    });
                });

                col.Item().Text("RECHNUNG").Bold().FontSize(20);

                col.Item().Row(row =>
                {
                    row.RelativeItem();
                    row.ConstantItem(260).Column(right =>
                    {
                        right.Spacing(2);
                        right.Item().Text($"Rechnungsnummer: {invoice.InvoiceNumber}");
                        right.Item().Text($"Rechnungsdatum: {invoice.InvoiceDate:dd.MM.yyyy}");
                        right.Item().Text($"Leistungszeitraum: {servicePeriodStart:dd.MM.yyyy} – {servicePeriodEnd:dd.MM.yyyy}");
                    });
                });

                col.Item().Column(c =>
                {
                    c.Spacing(2);
                    c.Item().Text("Leistungsempfänger:").SemiBold();
                    c.Item().Text(invoice.CustomerName);
                    c.Item().Text(invoice.CustomerAddress);
                    c.Item().Text(invoice.CustomerCity);
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(35);
                        columns.RelativeColumn(3);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.4f);
                    });

                    static IContainer HeaderCell(IContainer x) =>
                        x.BorderBottom(1).BorderColor(Colors.Grey.Lighten1).PaddingVertical(4).PaddingHorizontal(2);
                    static IContainer BodyCell(IContainer x) =>
                        x.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(2);

                    table.Header(header =>
                    {
                        header.Cell().Element(HeaderCell).Text("Pos.").SemiBold();
                        header.Cell().Element(HeaderCell).Text("Leistung").SemiBold();
                        header.Cell().Element(HeaderCell).AlignRight().Text("Menge").SemiBold();
                        header.Cell().Element(HeaderCell).AlignRight().Text("EP netto").SemiBold();
                        header.Cell().Element(HeaderCell).AlignRight().Text("Gesamt netto").SemiBold();
                    });

                    for (var index = 0; index < invoice.Items.Count; index++)
                    {
                        var item = invoice.Items[index];
                        table.Cell().Element(BodyCell).Text((index + 1).ToString(_culture));
                        table.Cell().Element(BodyCell).Text($"{item.Title} ({item.TaxRate:0.##}%)");
                        table.Cell().Element(BodyCell).AlignRight().Text(FormatQuantity(item.Quantity));
                        table.Cell().Element(BodyCell).AlignRight().Text(item.UnitPrice.ToString("C2", _culture));
                        table.Cell().Element(BodyCell).AlignRight().Text(item.NetTotal.ToString("C2", _culture));
                    }
                });

                col.Item().AlignRight().Width(260).Column(taxCol =>
                {
                    taxCol.Spacing(2);
                    foreach (var group in taxGroups)
                    {
                        taxCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"Netto {group.TaxRate:0.##}%:");
                            r.ConstantItem(100).AlignRight().Text(group.Net.ToString("C2", _culture));
                        });
                        taxCol.Item().Row(r =>
                        {
                            r.RelativeItem().Text($"MwSt {group.TaxRate:0.##}%:");
                            r.ConstantItem(100).AlignRight().Text(group.Tax.ToString("C2", _culture));
                        });
                        taxCol.Item().PaddingBottom(4);
                    }

                    taxCol.Item().PaddingTop(4).BorderTop(1).BorderColor(Colors.Grey.Lighten1).Row(r =>
                    {
                        r.RelativeItem().Text("Gesamt:").SemiBold();
                        r.ConstantItem(100).AlignRight().Text(invoice.TotalGross.ToString("C2", _culture)).SemiBold();
                    });
                });

                col.Item().PaddingTop(8).Text("Gemäß § 12 Abs. 2 Nr. 11 UStG gelten die ausgewiesenen Steuersätze je Leistungsart.");
                col.Item().Text("Zahlbar sofort ohne Abzug.");
            });
        }

        private string FormatQuantity(decimal quantity)
        {
            var hasFraction = quantity != decimal.Truncate(quantity);
            var formatted = quantity.ToString(hasFraction ? "N2" : "N0", _culture);
            return $"{formatted} Tage";
        }

        private sealed record TaxGroup(decimal TaxRate, decimal Net, decimal Tax);
    }
}
