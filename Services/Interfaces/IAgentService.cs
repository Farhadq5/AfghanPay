using AfghanPay.API.DTOs;

namespace AfghanPay.API.Services.Interfaces
{
    public interface IAgentService
    {
        Task<AgentDashboardDto> GetDashboardDtoAsync(Guid agentId);
    }
}
