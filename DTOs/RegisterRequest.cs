namespace AfghanPay.API.DTOs
{
    public class RegisterRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
        public string ConfirmPin { get; set; }= string.Empty;
    }

    public class RegisterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserInfo? User { get; set; }
    }
}
