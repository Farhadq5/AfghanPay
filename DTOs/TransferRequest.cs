namespace AfghanPay.API.DTOs
{
    public class TransferRequest
    {
        public string receiverPhone { get; set; } = string.Empty;
        public decimal amount { get; set; }
        public string pin { get; set; } = string.Empty;
    }

    public class  CashInRequest
    {
        public string CustomerPhoneNumber { get; set; } = string.Empty;
        public decimal amount { get; set; }
    }

    public class TransactionResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TransactionRef { get; set; }
        public string? NewBalance { get; set; }
        public string? Commission { get; set; }
    }
}
