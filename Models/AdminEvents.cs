using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace AfghanPay.Models
{
    [Table("admin_event")]
    public class AdminEvents
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Type { get; set; } = default!;

        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public Guid? ActoreAgentId { get; set; }
        public Guid? ActoreAdminId { get; set; }
        public Guid? TransactionId { get; set; }
        public Guid? CashoutRequestId { get; set; }
        public decimal? Amount { get; set; }
        public decimal? Fee { get; set; }
        public decimal? Commission { get; set; }
        public string? TxRef { get; set; }
        public string? SenderPhone { get; set; }
        public string? ReciverPhone { get; set; }
        public string? AgentCode { get; set; }
        public string? Staus { get; set; }
        public string? Reason { get; set; }
        public string? DataJson { get; set; }
    }
}