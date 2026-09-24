namespace al_ameer.Models
{
    public class Product
    {
        public int ProductId { get; set; }
        public string? Barcode { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }

        public decimal CostPrice { get; set; }
        public decimal SellingPrice { get; set; }
        public string Currency { get; set; } = "LBP";
        public decimal CostPriceUSD { get; set; }
        public decimal SellingPriceUSD { get; set; }
        public int? StockQuantity { get; set; }
        public bool? IsActive { get; set; }

        public bool? IsTire { get; set; }

        // Match SQL 'int' type from your script
        public int? TireWidth { get; set; }
        public int? TireRatio { get; set; }
        public int? TireDiameter { get; set; }

        // Navigation properties
        public virtual Category? Category { get; set; }
        public virtual Supplier? Supplier { get; set; }
    }
}
