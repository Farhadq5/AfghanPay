namespace AfghanPay.API.DTOs
{
    public class DashbordDto
    {

    }

    public class AgentDashboardDto
    {
        public decimal FloatBalance { get; set; }
        public decimal CommissionBalance { get; set; }
        public decimal TodayBalance { get; set; }
        public int TodayTransactions { get; set; }
        public decimal TodayVolume { get; set; }
    }

    public class AdminDashboardDto
    {
        public DateTime Date { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalFeesCollected { get; set; }
        public decimal TotalCommissionsPaid { get; set; }
        public decimal Profit { get; set; }
        public List<TransactionTypeCount> TransactionsByType { get; set; } = new();
    }

    public class TransactionTypeCount
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DailyReportDto
    {
        public DateTime Date { get; set; }
        public int TransactionCount { get; set; }
        public decimal TotalVolume { get; set; }
        public decimal TotalFees { get; set; }
        public decimal TotalCommissions { get; set; }
        public decimal Profit { get; set; }
    }

    public class AgentListDto
    {
        public Guid Id { get; set; }
        public string AgentCode { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public decimal FloatBalance { get; set; }
        public decimal CommissionBalance { get; set; }
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
