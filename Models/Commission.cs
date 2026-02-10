namespace AfghanPay.API.Models
{
    public class Commission
    {
        public Guid Id { get; set; }
        public Guid AgentId { get; set; }
        public Guid TransactionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        // foreign key relationships
        public Agent? Agent { get; set; }
        public Transaction? Transaction { get; set; }
    }
}
