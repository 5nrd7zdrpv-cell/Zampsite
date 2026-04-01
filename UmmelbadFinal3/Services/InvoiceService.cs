using System.IO;
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

        public string InvoiceDirectory => _invoiceDirectory;
    }
}
