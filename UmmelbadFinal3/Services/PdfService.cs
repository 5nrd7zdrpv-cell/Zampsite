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
                    page.DefaultTextStyle(x => x.FontFamily("Times New Roman").FontSize(10));

                    page.Content().Element(e => ComposeContent(e, invoice));
                });
            }).GeneratePdf(outputPath);

            return outputPath;
        }

        private void ComposeContent(IContainer container, Invoice invoice)
        {
            var servicePeriodStart = invoice.ServiceDate ?? invoice.InvoiceDate;
            var servicePeriodEnd = invoice.InvoiceDate;
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
                col.Spacing(10);

                col.Item().Row(row =>
                {
                    row.RelativeItem().Column(left =>
                    {
                        left.Spacing(1);
                        left.Item().Text("Waldcampingplatz Ummelbad").SemiBold();
                        left.Item().Text("Ummelweg 100");
                        left.Item().Text("27412 Hepstedt");
                        left.Item().Text("Tel.: 0152-57400199");
                        left.Item().Text("E-Mail: szampich@aol.com");
                        left.Item().Text("Web: www.waldcampingplatz-ummelbad.de");
                        left.Item().Text("Steuernummer: 52/150/03625");
                    });

                    row.ConstantItem(180).AlignRight().AlignTop().Element(c =>
                    {
                        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logo.png");
                        if (File.Exists(logoPath))
                        {
                            c.MaxWidth(180).Image(File.ReadAllBytes(logoPath), ImageScaling.FitWidth);
                        }
                    });
                });

                col.Item().PaddingTop(8).Text("RECHNUNG").Bold().FontSize(20);

                col.Item().Column(data =>
                {
                    data.Spacing(2);
                    data.Item().Text($"Rechnungsnummer: {invoice.InvoiceNumber}");
                    data.Item().Text($"Rechnungsdatum: {invoice.InvoiceDate:dd.MM.yyyy}");
                    data.Item().Text($"Leistungszeitraum: {servicePeriodStart:dd.MM.yyyy} – {servicePeriodEnd:dd.MM.yyyy}");
                });

                col.Item().PaddingTop(4).Column(c =>
                {
                    c.Spacing(2);
                    c.Item().Text("Leistungsempfänger:").SemiBold();
                    c.Item().Text(invoice.CustomerNameSnapshot);
                    c.Item().Text(invoice.Customer.Address);
                    c.Item().Text(invoice.Customer.City);
                });

                col.Item().PaddingTop(6).Table(table =>
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
                        x.Border(0.6f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(3);

                    static IContainer BodyCell(IContainer x) =>
                        x.BorderLeft(0.6f).BorderRight(0.6f).BorderBottom(0.6f).BorderColor(Colors.Grey.Lighten2).PaddingVertical(4).PaddingHorizontal(3);

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

                col.Item().AlignRight().Width(280).PaddingTop(6).Column(taxCol =>
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
                        taxCol.Item().PaddingBottom(2);
                    }

                    taxCol.Item().PaddingTop(4).BorderTop(0.8f).BorderColor(Colors.Grey.Lighten2).Row(r =>
                    {
                        r.RelativeItem().Text("Gesamt:").SemiBold();
                        r.ConstantItem(100).AlignRight().Text(invoice.TotalGross.ToString("C2", _culture)).SemiBold();
                    });
                });

                col.Item().PaddingTop(10).Column(bank =>
                {
                    bank.Spacing(1);
                    bank.Item().Text("Bankverbindung:").SemiBold();
                    bank.Item().Text("Volksbank Grasberg");
                    bank.Item().Text("IBAN: DE39 2916 2394 0711 4621 00");
                });

                col.Item().PaddingTop(8).Text("Gemäß § 12 Abs. 2 Nr. 11 UStG unterliegt die Vermietung von Campingflächen dem ermäßigten Steuersatz (7%).");
                col.Item().Text("Stromlieferungen unterliegen dem Regelsteuersatz von 19%.");
                col.Item().PaddingTop(2).Text("Zahlbar innerhalb 7 Tagen aufs angegebene Konto.");
            });
        }

        private string FormatQuantity(decimal quantity)
        {
            var hasFraction = quantity != decimal.Truncate(quantity);
            return quantity.ToString(hasFraction ? "N2" : "N0", _culture);
        }

        private sealed record TaxGroup(decimal TaxRate, decimal Net, decimal Tax);
    }
}
