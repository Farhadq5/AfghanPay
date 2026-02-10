namespace AfghanPay.API.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PinHash { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
