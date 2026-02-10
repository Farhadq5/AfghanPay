using AfghanPay.API.DTOs;
using AfghanPay.API.Services.Interfaces;
using AfghanPay.API.Data;
using Microsoft.EntityFrameworkCore;

namespace AfghanPay.API.Services
{
    public class AgentService : IAgentService
    {
        private readonly AppDbContext _dbcontext;
        public AgentService(AppDbContext dbcontext)
        {
            _dbcontext = dbcontext;
        }
        public async Task<AgentDashboardDto> GetDashboardDtoAsync(Guid agentId)
        {
            var agent = await _dbcontext.Agents.FirstOrDefaultAsync(a => a.Id == agentId);
            if (agent == null)
            {
                throw new Exception("Agent not found");
            }

            var today = DateTime.UtcNow.Date;

            // get today's commossion
            var todaycommission = await _dbcontext.Commissions.Where
                (c => c.AgentId == agentId && c.CreatedAt.Date == today)
                .SumAsync(c => (decimal?)c.Amount) ?? 0;

            // get todyas transactions count
            var todayTransactionsCount = await _dbcontext.Transactions
                .Where(t => t.AgentId == agentId && t.CreatedAt.Date == today)
                .CountAsync();

            // get today's transaction volume
            var todayTransactionVolume = await _dbcontext.Transactions
                .Where(t => t.AgentId == agentId && t.CreatedAt.Date == today)
                .SumAsync(t => (decimal?)t.Amount) ?? 0;

            return new AgentDashboardDto
            {
                FloatBalance = agent.FloatBalance,
                CommissionBalance = agent.CommissionBalance,
                TodayBalance = todaycommission,
                TodayTransactions = todayTransactionsCount,
                TodayVolume = todayTransactionVolume
            };
        }
    }
}
