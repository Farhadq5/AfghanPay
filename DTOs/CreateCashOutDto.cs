using AfghanPay.API.Models;
using System.Security;

namespace AfghanPay.API.DTOs
{
    //users create cash out request
    public class CreateCashOutDto
    {
        public string AgentCode { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string pin { get; set; } = string.Empty;
    }

    //agent response
    public class RespondeCashoutDto
    {
        public Guid RequestId { get; set; }
        public bool IsApproved { get; set; }
        public string RejectionMessage { get; set; } = string.Empty;

    }

    //response to user
    public class CashoutResponseDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string UserPhone { get; set; } = string.Empty;
        public string AgentCode { get; set; } = string.Empty;
        public string? AgentName { get; set; }
        public decimal Amount { get; set; }
        public decimal fee { get; set; }
        public CashoutStatus Status { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public string? RejectionReason { get; set; }
        public string TransactionRef { get; set; } = string.Empty;

    }

    public class CashOutTransactionResponse
    {
        public bool success { get; set; }
        public string message { get; set; } = string.Empty;
        public string? transactionRef { get; set; } = string.Empty;
        public string? newBalance { get; set; }
        public CashoutResponseDto? Data { get; set; }
    }
}
