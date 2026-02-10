using Microsoft.AspNetCore.Identity;

namespace AfghanPay.API.DTOs
{
    public class LoginRequest
    {
        public string PhoneNumber { get; set; } = string.Empty;
        public string Pin { get; set; } = string.Empty;
    }

    public class AdminLoginRequest
    {
        public string UserName { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class LoginResponse
    {
        public bool Success { get; set; }
        public string Token { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public UserInfo? User { get; set; }
    }

public class UserInfo
{
    public Guid Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public decimal Balance { get; set; }
    public bool IsAgent { get; set; }
    public string? AgentCode { get; set; }
}
}
