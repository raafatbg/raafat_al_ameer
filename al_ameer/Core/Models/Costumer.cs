namespace al_ameer.Models
{
    public class Customer
    {
        public int CustomerId { get; set; }
        public required string FullName { get; set; }
        public required string Phone { get; set; }
        public DateTime RegistrationDate { get; set; }
    }
}