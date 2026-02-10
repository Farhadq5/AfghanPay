namespace AfghanPay.API.Models
{
    public class Agent
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string AgentCode { get; set; } = string.Empty;
        public decimal FloatBalance { get; set; }
        public decimal CommissionBalance { get; set; }
        public string Status { get; set; } = "Active";
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // foreign key relationship
        public User? User { get; set; }
    }
}
