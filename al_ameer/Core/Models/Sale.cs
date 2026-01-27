using System;
using System.Collections.Generic;

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

        // Use nullable decimal? to prevent "Data is Null" errors if DB has nulls
        public decimal? GrandTotal { get; set; }

        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }

        // FIXED: Added Navigation Property to link to Customer table
        public virtual Customer? Customer { get; set; }

        // Navigation Property for items sold in this transaction
        public virtual ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    }
}