namespace al_ameer.Models
{
    public class SaleItem
    {
        public int SaleItemId { get; set; }
        public int SaleId { get; set; }
        public int? ProductId { get; set; }
        public int? ServiceId { get; set; }
        public string ItemType { get; set; } = "Product";
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal LineTotal { get; set; }

        public virtual Sale Sale { get; set; } = null!;
        public virtual Product? Product { get; set; }
    }
}
