using AfghanPay.API.Data;
using AfghanPay.API.DTOs;
using AfghanPay.API.Models;
using AfghanPay.API.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AfghanPay.API.Services
{
    public class TransactionService : ITransactionService
    {
        private readonly AppDbContext _dbcontext;

        public TransactionService(AppDbContext context)
        {
            _dbcontext = context;
        }

        public async Task<List<Transaction>> GetAgentTransactionsAsync(Guid agentId, int limit = 50)
        {
           return await _dbcontext.Transactions
                .Where(t => t.AgentId == agentId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public  async Task<List<Transaction>> GetUserTransactionsAsync(Guid userId, int limit = 50)
        {
            return  await _dbcontext.Transactions
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
                // get agent
                var agent = await _dbcontext.Agents.FindAsync(agentId);
                if (agent == null)
                {
                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Agent not found."
                    };
                }
                // get customer
                var customer = await _dbcontext.Users.FirstOrDefaultAsync(u => u.PhoneNumber == customerPhone);
                if (customer == null)
                {
                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Customer not found."
                    };
                }
                // cheack agent float balance
                if (agent.FloatBalance < amount)
                {
                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Agent has insufficient float balance."
                    };
                }
                // update balances
                agent.FloatBalance -= amount;
                customer.Balance += amount;

                // create transaction record
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
                // get sender
                var sender = await _dbcontext.Users.FindAsync(senderId);
                if (sender == null)
                {
                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Sender not found."
                    };
                }
                // verify pin
                if (pin != sender.PinHash)
                {
                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Invalid PIN."
                    };
                }
                // get receiver
                var receiver = await _dbcontext.Users.FirstOrDefaultAsync(x => x.PhoneNumber == receiverPhone);
                if (receiver == null)
                {
                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Receiver not found."
                    };
                }
                // cant send to self
                if (sender.Id == receiver.Id)
                {
                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Cannot transfer to self."
                    };
                }

                // calculate fee
                var feeStructure = await _dbcontext.Fees.FirstOrDefaultAsync(f => f.TransactionType == "p2p_transfer");
                var fee = CalculateFee(amount, feeStructure);
                var totalDebit = amount + fee;
                // check sender balance
                if (sender.Balance < totalDebit)
                {
                    return new TransactionResponse
                    {
                        Success = false,
                        Message = "Insufficient balance."
                    };
                }
                // update balances
                sender.Balance -= totalDebit;
                receiver.Balance += amount;

                // create transaction record
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

                return new TransactionResponse
                {
                    Success = true,
                    Message = "Transfer successful.",                  
                    TransactionRef = txn.TransactionRef,

                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return new TransactionResponse
                {
                    Success = false,
                    Message = $"An error occurred during the transaction:{ex.Message}"
                };

            }
        }
        #endregion

        //helper metods
        private decimal CalculateFee(decimal amount, Fee? feeStructure)
        {
            if(feeStructure == null)           
                return 0;

            var CalculatedFee = amount * feeStructure.FeePercentage;
            return Math.Max(CalculatedFee, feeStructure.MinFee);
        }

        private string GenerateTransactionRef()
        {
            var date = DateTime.UtcNow.ToString("yyyymmdd");
            var random = Guid.NewGuid().ToString().Substring(0, 6).ToUpper();
            return $"TXN{date}{random}";
        }
    }
}
