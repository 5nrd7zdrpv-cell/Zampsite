using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using UmmelbadFinal3.Models;

namespace UmmelbadFinal3.Services
{
    public class InvoiceService
    {
        private readonly string _invoiceDirectory;

        public InvoiceService(string baseDirectory)
        {
            _invoiceDirectory = Path.Combine(baseDirectory, "Invoices");
            Directory.CreateDirectory(_invoiceDirectory);
        }

        public string Save(Invoice invoice)
        {
            var fileName = $"Invoice_{invoice.InvoiceNumber}.json";
            var path = Path.Combine(_invoiceDirectory, fileName);
            var json = JsonSerializer.Serialize(invoice, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(path, json);
            return path;
        }

        public Invoice? Load(string path)
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<Invoice>(json);
        }

        public List<Invoice> LoadAll()
        {
            var invoices = new List<Invoice>();
            foreach (var file in Directory.GetFiles(_invoiceDirectory, "*.json"))
            {
                try
                {
                    var invoice = Load(file);
                    if (invoice != null)
                    {
                        invoices.Add(invoice);
                    }
                }
                catch
                {
                    // Ignoriere defekte Dateien in der Übersicht.
                }
            }

            return invoices
                .OrderByDescending(i => i.InvoiceDate)
                .ThenBy(i => i.CustomerNameSnapshot)
                .ToList();
        }

        public string? FindInvoicePathByNumber(string invoiceNumber)
        {
            var safeInvoiceNumber = invoiceNumber.Trim();
            if (string.IsNullOrWhiteSpace(safeInvoiceNumber))
            {
                return null;
            }

            var exactPath = Path.Combine(_invoiceDirectory, $"Invoice_{safeInvoiceNumber}.json");
            if (File.Exists(exactPath))
            {
                return exactPath;
            }

            return Directory.GetFiles(_invoiceDirectory, "*.json")
                .FirstOrDefault(path => string.Equals(
                    Load(path)?.InvoiceNumber,
                    safeInvoiceNumber,
                    StringComparison.OrdinalIgnoreCase));
        }

        public string InvoiceDirectory => _invoiceDirectory;
    }
}
