namespace AfghanPay.API.Models
{
    public class Fee
    {
        public Guid Id { get; set; }
        public string TransactionType { get; set; } = string.Empty;
        public decimal FeePercentage { get; set; }
        public decimal MinFee { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
