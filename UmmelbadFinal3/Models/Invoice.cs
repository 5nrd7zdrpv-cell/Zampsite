using System;
using System.Collections.Generic;
using System.Linq;

namespace UmmelbadFinal3.Models
{
    public class Invoice
    {
        public string InvoiceNumber { get; set; } = string.Empty;
        public DateTime InvoiceDate { get; set; } = DateTime.Today;
        public DateTime? ServiceDate { get; set; }

        public Customer Customer { get; set; } = new();
        public string CustomerNameSnapshot { get; set; } = string.Empty;

        public List<InvoiceItem> Items { get; set; } = new();
        public List<int> IncludedBookingIds { get; set; } = new();
        public List<int> IncludedCafeSaleIds { get; set; } = new();

        public decimal TotalNet => Items.Sum(i => i.NetTotal);
        public decimal TotalTax => Items.Sum(i => i.TaxAmount);
        public decimal TotalGross => Items.Sum(i => i.GrossTotal);
    }
}
