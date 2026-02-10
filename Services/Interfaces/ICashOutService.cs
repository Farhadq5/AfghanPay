using AfghanPay.API.DTOs;

namespace AfghanPay.API.Services.Interfaces
{
    public interface ICashOutService
    {

        Task<CashOutTransactionResponse> CreateCashOutRequestAsync(Guid userId, CreateCashOutDto cashoutrequest);
        Task<CashOutTransactionResponse> RespondeToRequestAsync(Guid agentId ,RespondeCashoutDto responseCashout);
        Task<CashOutTransactionResponse> CompleteRequestAsync(Guid agentId, Guid cashoutRequestId);
        Task<List<CashoutResponseDto>> GetPendingForAgentAsync(Guid agentId);
        Task<List<CashoutResponseDto>> GetUserHistoryAsync(Guid UserId);
        Task<List<CashoutResponseDto>> GetApprovedForAgentAsync( Guid UserId);
        Task<List<CashoutResponseDto>> GetAgentHistoryAsync(Guid AgentId);

    }
}
