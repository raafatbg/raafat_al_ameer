namespace al_ameer.Models
{
    public class Category
    {
        public int CategoryId { get; set; }
        public required string CategoryName { get; set; }
        public string? Description { get; set; }

        // Relationship: One category can have many products
        public virtual required ICollection<Product> Products { get; set; }
    }
}