using AfghanPay.API.Data;
using AfghanPay.API.DTOs;
using AfghanPay.API.Models;
using AfghanPay.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AfghanPay.API.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _dbcontext;
        public AdminService(AppDbContext context)
        {
            _dbcontext = context;
        }

        // Dashboard Statistics
        public async Task<AdminDashboardDto> GetDashboardAsync()
        {
            var today = DateTime.UtcNow.Date;

            var todayTransactions = await _dbcontext.Transactions
                .Where(t => t.CreatedAt.Date == today)
                .ToListAsync();

            var totalVolume = todayTransactions.Sum(t => t.Amount);
            var totalFees = todayTransactions.Sum(t => t.Fee);

            var totalCommissions = await _dbcontext.Commissions
                .Where(c => c.CreatedAt.Date == today)
                .SumAsync(c => (decimal?)c.Amount) ?? 0;

            var profit = totalFees - totalCommissions;

            var transactionsByType = todayTransactions
                .GroupBy(t => t.Type)
                .Select(g => new TransactionTypeCount
                {
                    Type = g.Key,
                    Count = g.Count()
                })
                .ToList();

            return new AdminDashboardDto
            {
                Date = today,
                TotalTransactions = todayTransactions.Count,
                TotalVolume = totalVolume,
                TotalFeesCollected = totalFees,
                TotalCommissionsPaid = totalCommissions,
                Profit = profit,
                TransactionsByType = transactionsByType
            };
        }

        // Get All Transactions (Paginated)
        public async Task<List<Transaction>> GetAllTransactionsAsync(int page = 1, int pageSize = 50)
        {
            return await _dbcontext.Transactions
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        // Daily Report
        public async Task<List<DailyReportDto>> GetDailyReportAsync(int days = 7)
        {
            var startDate = DateTime.UtcNow.Date.AddDays(-days);

            var transactions = await _dbcontext.Transactions
                .Where(t => t.CreatedAt >= startDate)
                .ToListAsync();

            var commissions = await _dbcontext.Commissions
                .Where(c => c.CreatedAt >= startDate)
                .ToListAsync();

            var report = transactions
                .GroupBy(t => t.CreatedAt.Date)
                .Select(g => new DailyReportDto
                {
                    Date = g.Key,
                    TransactionCount = g.Count(),
                    TotalVolume = g.Sum(t => t.Amount),
                    TotalFees = g.Sum(t => t.Fee),
                    TotalCommissions = commissions
                        .Where(c => c.CreatedAt.Date == g.Key)
                        .Sum(c => c.Amount),
                    Profit = g.Sum(t => t.Fee) - commissions
                        .Where(c => c.CreatedAt.Date == g.Key)
                        .Sum(c => c.Amount)
                })
                .OrderByDescending(r => r.Date)
                .ToList();

            return report;
        }

        // Get All Users
        public async Task<List<User>> GetAllUsersAsync()
        {
            return await _dbcontext.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        // Get All Agents
        public async Task<List<AgentListDto>> GetAllAgentsAsync()
        {
            var agents = await _dbcontext.Agents
                .Include(a => a.User)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return agents.Select(a => new AgentListDto
            {
                Id = a.Id,
                AgentCode = a.AgentCode,
                FullName = a.User?.FullName ?? "",
                PhoneNumber = a.User?.PhoneNumber ?? "",
                FloatBalance = a.FloatBalance,
                CommissionBalance = a.CommissionBalance,
                Status = a.Status,
                CreatedAt = a.CreatedAt
            }).ToList();
        }
    }
}
