using AfghanPay.API.DTOs;
using AfghanPay.API.Models;

namespace AfghanPay.API.Services.Interfaces
{
    public interface IAdminService
    {
        Task<AdminDashboardDto> GetDashboardAsync();
        Task<List<Transaction>> GetAllTransactionsAsync(int page = 1, int pageSize = 50);
        Task<List<DailyReportDto>> GetDailyReportAsync(int days = 7);
        Task<List<User>> GetAllUsersAsync();
        Task<List<AgentListDto>> GetAllAgentsAsync();
    }
}
