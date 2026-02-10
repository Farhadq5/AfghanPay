namespace AfghanPay.API.Models
{
    public class Transaction
    {
        public Guid Id { get; set; }
        public string TransactionRef { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public Guid? SenderId { get; set; }
        public Guid? ReceiverId { get; set; }
        public Guid? AgentId { get; set; }
        public decimal Amount { get; set; }
        public decimal Fee { get; set; }
        public string Status { get; set; } = "Completed"; // e.g., "Pending", "Completed", "Failed"
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // foreign key relationships
        public User? Sender { get; set; }
        public User? Receiver { get; set; }
        public User? Agent { get; set; }
    }
}
