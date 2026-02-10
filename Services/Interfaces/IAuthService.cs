using AfghanPay.API.DTOs;

namespace AfghanPay.API.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> UserLoginAsync(LoginRequest request);
        Task<LoginResponse> AdminLoginAsync(AdminLoginRequest request);
        Task<RegisterResponse> RegisterUserAsync(RegisterRequest request);
        string GenerateJwtToken(Guid userid,string role, string? agentCode = null);
    }
}
