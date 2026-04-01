using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UmmelbadFinal3.Models;

namespace UmmelbadFinal3.Services
{
    public class InvoiceService
    {
        private readonly DataService _dataService;
        private readonly string _invoiceDirectory;
        private readonly string _draftPath;

        public InvoiceService(string baseDirectory, DataService? dataService = null)
        {
            _dataService = dataService ?? new DataService();
            _invoiceDirectory = Path.Combine(baseDirectory, "Invoices");
            Directory.CreateDirectory(_invoiceDirectory);
            _draftPath = Path.Combine(_invoiceDirectory, "invoice_draft.json");
        }

        public string Save(Invoice invoice)
        {
            var fileName = $"Invoice_{invoice.InvoiceNumber}.json";
            var path = Path.Combine(_invoiceDirectory, fileName);
            _dataService.Save(path, invoice);
            return path;
        }

        public Invoice? Load(string path)
        {
            return _dataService.Load<Invoice?>(path, null);
        }

        public void SaveDraft(Invoice invoice) => _dataService.Save(_draftPath, invoice);

        public Invoice? LoadDraft() => _dataService.Load<Invoice?>(_draftPath, null);

        public void DeleteDraft()
        {
            if (File.Exists(_draftPath))
            {
                File.Delete(_draftPath);
            }
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
