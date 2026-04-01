using System;

namespace UmmelbadFinal3.Models
{
    public class InvoiceItem
    {
        public string Title { get; set; } = string.Empty;
        public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }

        public decimal NetTotal => Math.Round(Quantity * UnitPrice, 2);
        public decimal TaxAmount => Math.Round(NetTotal * (TaxRate / 100m), 2);
        public decimal GrossTotal => NetTotal + TaxAmount;
    }
}
