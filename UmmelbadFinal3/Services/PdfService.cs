using System;
using System.Globalization;
using System.IO;
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
            var logoPath = Path.Combine(_baseDirectory, "Logo.png");
            byte[]? logoBytes = File.Exists(logoPath) ? File.ReadAllBytes(logoPath) : null;

            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(24);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(e => ComposeHeader(e, invoice, logoBytes));
                    page.Content().Element(e => ComposeContent(e, invoice));
                    page.Footer().AlignCenter().Text("Vielen Dank für Ihren Auftrag.").FontSize(9).FontColor(Colors.Grey.Darken1);
                });
            }).GeneratePdf(outputPath);

            return outputPath;
        }

        private void ComposeHeader(IContainer container, Invoice invoice, byte[]? logoBytes)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(col =>
                {
                    col.Item().Text("Ihre Firma GmbH").SemiBold().FontSize(14);
                    col.Item().Text($"Rechnung: {invoice.InvoiceNumber}");
                    col.Item().Text($"Rechnungsdatum: {invoice.InvoiceDate:dd.MM.yyyy}");
                    if (invoice.ServiceDate.HasValue)
                    {
                        col.Item().Text($"Leistungsdatum: {invoice.ServiceDate.Value:dd.MM.yyyy}");
                    }
                });

                row.ConstantItem(140).Height(70).AlignRight().AlignMiddle().Element(c =>
                {
                    if (logoBytes != null)
                    {
                        c.Image(logoBytes, ImageScaling.FitArea);
                    }
                });
            });
        }

        private void ComposeContent(IContainer container, Invoice invoice)
        {
            container.Column(col =>
            {
                col.Spacing(12);
                col.Item().Border(1).Padding(8).Column(c =>
                {
                    c.Item().Text("Kundendaten").SemiBold();
                    c.Item().Text(invoice.CustomerName);
                    c.Item().Text(invoice.CustomerAddress);
                    c.Item().Text(invoice.CustomerCity);
                    if (!string.IsNullOrWhiteSpace(invoice.CustomerEmail)) c.Item().Text($"E-Mail: {invoice.CustomerEmail}");
                    if (!string.IsNullOrWhiteSpace(invoice.CustomerPhone)) c.Item().Text($"Telefon: {invoice.CustomerPhone}");
                });

                col.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.RelativeColumn(3);
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                        columns.RelativeColumn();
                    });

                    table.Header(header =>
                    {
                        header.Cell().Text("Titel").SemiBold();
                        header.Cell().AlignRight().Text("Menge").SemiBold();
                        header.Cell().AlignRight().Text("EP").SemiBold();
                        header.Cell().AlignRight().Text("UST %").SemiBold();
                        header.Cell().AlignRight().Text("Netto").SemiBold();
                        header.Cell().AlignRight().Text("Brutto").SemiBold();
                    });

                    foreach (var item in invoice.Items)
                    {
                        table.Cell().Text(item.Title);
                        table.Cell().AlignRight().Text(item.Quantity.ToString("N2", _culture));
                        table.Cell().AlignRight().Text(item.UnitPrice.ToString("C2", _culture));
                        table.Cell().AlignRight().Text(item.TaxRate.ToString("N2", _culture));
                        table.Cell().AlignRight().Text(item.NetTotal.ToString("C2", _culture));
                        table.Cell().AlignRight().Text(item.GrossTotal.ToString("C2", _culture));
                    }
                });

                col.Item().AlignRight().Width(220).Table(t =>
                {
                    t.ColumnsDefinition(c => { c.RelativeColumn(); c.RelativeColumn(); });
                    t.Cell().Text("Netto:");
                    t.Cell().AlignRight().Text(invoice.TotalNet.ToString("C2", _culture));
                    t.Cell().Text("Steuer:");
                    t.Cell().AlignRight().Text(invoice.TotalTax.ToString("C2", _culture));
                    t.Cell().Text("Brutto:").SemiBold();
                    t.Cell().AlignRight().Text(invoice.TotalGross.ToString("C2", _culture)).SemiBold();
                });
            });
        }
    }
}
