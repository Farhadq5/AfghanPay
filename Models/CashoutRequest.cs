using Microsoft.AspNetCore.Mvc.ViewFeatures;

namespace AfghanPay.API.Models
{

    public enum CashoutStatus
    {
        Pending = 0,
        Approved = 1,
        Rejected = 2,
        Completed = 3
    }
    public class CashoutRequest
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; } 
        public string AgentCode { get; set; } = string.Empty;
        public Guid? AgentId { get; set; }
        public decimal Amount { get; set; }
        public decimal fee { get; set; }
        public CashoutStatus Status { get; set; } = CashoutStatus.Pending;
        public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
        public string? RejectionMessage { get; set; }
        public string TransactionRef { get; set; }


        public User? User { get; set; }
        public Agent? Agent { get; set; }

    }


}
