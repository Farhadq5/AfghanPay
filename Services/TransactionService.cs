using AfghanPay.API.Data;
using AfghanPay.API.DTOs;
using AfghanPay.API.Models;
using AfghanPay.API.Services.Interfaces;
using AfghanPay.Models;
using AfghanPay.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AfghanPay.API.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _dbcontext;
        private readonly IAdminEventsBrodcaster _adminEventsBrodcaster;
        private readonly ILogger<TransactionService> _logger;

        public TransactionService(
            AppDbContext context,
            IAdminEventsBrodcaster adminEventsBrodcaster,
            ILogger<TransactionService> logger)
        {
            _dbcontext = context;
            _adminEventsBrodcaster = adminEventsBrodcaster;
            _logger = logger;
        }

        public async Task<List<Transaction>> GetAgentTransactionsAsync(Guid agentId, int limit = 50)
        {
            return await _dbcontext.Transactions
                .Where(t => t.AgentId == agentId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<List<Transaction>> GetUserTransactionsAsync(Guid userId, int limit = 50)
        {
            return await _dbcontext.Transactions
                .Where(t => t.SenderId == userId || t.ReceiverId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        #region Cash in
        public async Task<TransactionResponse> ProcessCashInAsync(Guid agentId, string customerPhone, decimal amount)
        {
            using var transaction = await _dbcontext.Database.BeginTransactionAsync();

            try
            {
                var agent = await _dbcontext.Agents
                    .Include(a => a.User)
                    .FirstOrDefaultAsync(a => a.Id == agentId);

                if (agent == null)
                {
                    await TryLogAdminEventAsync(new AdminEvents
                    {
                        Type = "cash_in",
                        Staus = "failed",
                        Reason = "Agent not found",
                        ActoreAgentId = agentId,
                        Amount = amount,
                        ReciverPhone = customerPhone
                    });

                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Agent not found."
                    };
                }

                var customer = await _dbcontext.Users.FirstOrDefaultAsync(u => u.PhoneNumber == customerPhone);
                if (customer == null)
                {
                    await TryLogAdminEventAsync(new AdminEvents
                    {
                        Type = "cash_in",
                        Staus = "failed",
                        Reason = "Customer not found",
                        ActoreAgentId = agent.Id,
                        Amount = amount,
                        AgentCode = agent.AgentCode,
                        SenderPhone = agent.User?.PhoneNumber,
                        ReciverPhone = customerPhone
                    });

                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Customer not found."
                    };
                }

                if (agent.FloatBalance < amount)
                {
                    await TryLogAdminEventAsync(new AdminEvents
                    {
                        Type = "cash_in",
                        Staus = "failed",
                        Reason = "Agent has insufficient float balance",
                        ActoreAgentId = agent.Id,
                        Amount = amount,
                        AgentCode = agent.AgentCode,
                        SenderPhone = agent.User?.PhoneNumber,
                        ReciverPhone = customer.PhoneNumber
                    });

                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Agent has insufficient float balance."
                    };
                }

                agent.FloatBalance -= amount;
                customer.Balance += amount;

                var txn = new Transaction
                {
                    Id = Guid.NewGuid(),
                    TransactionRef = GenerateTransactionRef(),
                    Type = "Cash_in",
                    SenderId = agent.UserId,
                    ReceiverId = customer.Id,
                    AgentId = agent.Id,
                    Amount = amount,
                    Fee = 0,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow
                };

                _dbcontext.Transactions.Add(txn);
                await _dbcontext.SaveChangesAsync();
                await transaction.CommitAsync();

                await TryLogAdminEventAsync(new AdminEvents
                {
                    Type = "cash_in",
                    Staus = "completed",
                    ActoreAgentId = agent.Id,
                    TransactionId = txn.Id,
                    Amount = txn.Amount,
                    Fee = txn.Fee,
                    TxRef = txn.TransactionRef,
                    SenderPhone = agent.User?.PhoneNumber,
                    ReciverPhone = customer.PhoneNumber,
                    AgentCode = agent.AgentCode
                });

                return new TransactionResponse
                {
                    Success = true,
                    Message = "Cash-in successful.",
                    TransactionRef = txn.TransactionRef,
                    NewBalance = agent.FloatBalance.ToString("F2")
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                await TryLogAdminEventAsync(new AdminEvents
                {
                    Type = "cash_in",
                    Staus = "failed",
                    Reason = ex.Message,
                    ActoreAgentId = agentId,
                    Amount = amount,
                    ReciverPhone = customerPhone
                });

                return new TransactionResponse
                {
                    Success = false,
                    Message = $"An error occurred during the transaction: {ex.Message}"
                };
            }
        }
        #endregion

        #region P2P transfer
        public async Task<TransactionResponse> ProcessP2PTransferAsync(Guid senderId, string receiverPhone, decimal amount, string pin)
        {
            using var transaction = await _dbcontext.Database.BeginTransactionAsync();

            try
            {
                var sender = await _dbcontext.Users.FindAsync(senderId);
                if (sender == null)
                {
                    await TryLogAdminEventAsync(new AdminEvents
                    {
                        Type = "p2p_transfer",
                        Staus = "failed",
                        Reason = "Sender not found",
                        Amount = amount,
                        ReciverPhone = receiverPhone
                    });

                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Sender not found."
                    };
                }

                if (pin != sender.PinHash)
                {
                    await TryLogAdminEventAsync(new AdminEvents
                    {
                        Type = "p2p_transfer",
                        Staus = "failed",
                        Reason = "Invalid PIN",
                        Amount = amount,
                        SenderPhone = sender.PhoneNumber,
                        ReciverPhone = receiverPhone
                    });

                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Invalid PIN."
                    };
                }

                var receiver = await _dbcontext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == receiverPhone);
                if (receiver == null)
                {
                    await TryLogAdminEventAsync(new AdminEvents
                    {
                        Type = "p2p_transfer",
                        Staus = "failed",
                        Reason = "Receiver not found",
                        Amount = amount,
                        SenderPhone = sender.PhoneNumber,
                        ReciverPhone = receiverPhone
                    });

                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Receiver not found."
                    };
                }

                if (sender.Id == receiver.Id)
                {
                    await TryLogAdminEventAsync(new AdminEvents
                    {
                        Type = "p2p_transfer",
                        Staus = "failed",
                        Reason = "Cannot transfer to self",
                        Amount = amount,
                        SenderPhone = sender.PhoneNumber,
                        ReciverPhone = receiver.PhoneNumber
                    });

                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Cannot transfer to self."
                    };
                }

                var feeStructure = await _dbcontext.Fees.FirstOrDefaultAsync(f => f.TransactionType == "p2p_transfer");
                var fee = CalculateFee(amount, feeStructure);
                var totalDebit = amount + fee;

                if (sender.Balance < totalDebit)
                {
                    await TryLogAdminEventAsync(new AdminEvents
                    {
                        Type = "p2p_transfer",
                        Staus = "failed",
                        Reason = "Insufficient balance",
                        Amount = amount,
                        Fee = fee,
                        SenderPhone = sender.PhoneNumber,
                        ReciverPhone = receiver.PhoneNumber
                    });

                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Insufficient balance."
                    };
                }

                sender.Balance -= totalDebit;
                receiver.Balance += amount;

                var txn = new Transaction
                {
                    Id = Guid.NewGuid(),
                    TransactionRef = GenerateTransactionRef(),
                    Type = "P2P_transfer",
                    SenderId = sender.Id,
                    ReceiverId = receiver.Id,
                    Amount = amount,
                    Fee = fee,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow
                };

                _dbcontext.Transactions.Add(txn);
                await _dbcontext.SaveChangesAsync();
                await transaction.CommitAsync();

                await TryLogAdminEventAsync(new AdminEvents
                {
                    Type = "p2p_transfer",
                    Staus = "completed",
                    TransactionId = txn.Id,
                    Amount = txn.Amount,
                    Fee = txn.Fee,
                    TxRef = txn.TransactionRef,
                    SenderPhone = sender.PhoneNumber,
                    ReciverPhone = receiver.PhoneNumber
                });

                return new TransactionResponse
                {
                    Success = true,
                    Message = "Transfer successful.",
                    TransactionRef = txn.TransactionRef
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();

                await TryLogAdminEventAsync(new AdminEvents
                {
                    Type = "p2p_transfer",
                    Staus = "failed",
                    Reason = ex.Message,
                    Amount = amount,
                    ReciverPhone = receiverPhone
                });

                return new TransactionResponse
                {
                    Success = false,
                    Message = $"An error occurred during the transaction:{ex.Message}"
                };
            }
        }
        #endregion

        private decimal CalculateFee(decimal amount, Fee? feeStructure)
        {
            if (feeStructure == null)
            {
                return 0;
            }

            var calculatedFee = amount * feeStructure.FeePercentage;
            return Math.Max(calculatedFee, feeStructure.MinFee);
        }

        private string GenerateTransactionRef()
        {
            var date = DateTime.UtcNow.ToString("yyyymmdd");
            var random = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            return $"TXN{date}{random}";
        }

        private async Task TryLogAdminEventAsync(AdminEvents adminEvent)
        {
            try
            {
                await _adminEventsBrodcaster.SaveAndBrodcastAsync(adminEvent);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to log/broadcast admin event. Type: {EventType}", adminEvent.Type);
            }
        }
    }
}
