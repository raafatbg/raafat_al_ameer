using System;
using System.Collections.Generic;
using System.Net.ServerSentEvents;

namespace al_ameer.Models
{
    public class Sale
    {
        public int SaleId { get; set; }
        public string? InvoiceNumber { get; set; }
        public int? CustomerId { get; set; }
        public DateTime SaleDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal GrandTotal { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }

        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}