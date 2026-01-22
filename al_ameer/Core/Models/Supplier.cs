namespace al_ameer.Models
{
    public class Supplier
    {
        public int SupplierId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Relationship: One supplier can provide many products
        // Removed 'required' here as it interferes with EF Core's collection initialization
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}