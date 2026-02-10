using AfghanPay.API.DTOs;
using AfghanPay.API.Models;

namespace AfghanPay.API.Services.Interfaces
{
    public interface ITransactionService
    {
        Task<TransactionResponse> ProcessP2PTransferAsync(Guid senderId, string receiverPhone, decimal amount, string pin);
        Task<TransactionResponse> ProcessCashInAsync(Guid agentId, string customerPhone, decimal amount);
        //Task<TransactionResponse> ProcessCashOutAsync(Guid agentId, string customerPhone, decimal amount);
        Task<List<Transaction>> GetUserTransactionsAsync(Guid userId, int limit = 50);
        Task<List<Transaction>> GetAgentTransactionsAsync(Guid agentId, int limit = 50);

    }
}
