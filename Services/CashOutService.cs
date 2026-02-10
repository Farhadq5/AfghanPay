using AfghanPay.API.Data;
using AfghanPay.API.DTOs;
using AfghanPay.API.Hubs;
using AfghanPay.API.Models;
using AfghanPay.API.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AfghanPay.API.Services
{
    public class CashOutService : ICashOutService
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<CashoutHub> _hubContext;
        private readonly IHubContext<NotificationHub> _notificationHub;
        public CashOutService(AppDbContext dbContext, IHubContext<CashoutHub> hubContext,
            IHubContext<NotificationHub> notocation)
        {
            _context = dbContext;
            _hubContext = hubContext;
            _notificationHub = notocation;
        }
       
        async Task<CashOutTransactionResponse> ICashOutService.CreateCashOutRequestAsync(Guid userId, CreateCashOutDto cashoutrequest)
        {
            try
            {
                //validate user and pin
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
                if (user == null)
                {
                    return new CashOutTransactionResponse
                    {
                        success = false,
                        message = "User not found"
                    };
                }
                if (user.PinHash != cashoutrequest.pin)
                {
                    return new CashOutTransactionResponse
                    {
                        success = false,
                        message = "Invalid PIN"
                    };
                }

                

                //validate agent
                var agent = await _context.Agents.Include
                                (a => a.User).FirstOrDefaultAsync
                                (a => a.AgentCode == cashoutrequest.AgentCode);

                if (agent == null)
                {
                    return new CashOutTransactionResponse
                    {
                        success = false,
                        message = "Invalid agent code"
                    };
                }
                //calculate fee

                var feeStructure = await _context.Fees.FirstOrDefaultAsync(f => f.TransactionType == "cash_out");
                var fee = CalculateFee(cashoutrequest.Amount, feeStructure);

                var totalDeduction = cashoutrequest.Amount + fee;
                //check balance
                if (user.Balance <totalDeduction)
                {
                    return new CashOutTransactionResponse
                    {
                        success = false,
                        message = "Insufficient balance"
                    };
                }





                // create cashout request

                var cashout = new CashoutRequest
                {
                    Id = Guid.NewGuid(),
                    UserId = userId,
                    AgentCode = cashoutrequest.AgentCode,
                    AgentId = agent.Id,
                    Amount = cashoutrequest.Amount,
                    fee = fee,
                    Status = CashoutStatus.Pending,
                    RequestedAt = DateTime.UtcNow,
                    TransactionRef = GenerateTransactionRef()
                };

                _context.Add(cashout);

                // deduct balance
                user.Balance -= totalDeduction;

                await _context.SaveChangesAsync();


                var dto = MapToDto(cashout, user, agent.User);

                await _hubContext.Clients.Group(agent.AgentCode.Trim().ToUpper())
                    .SendAsync("newCashoutRequest", dto);

                return new CashOutTransactionResponse
                {
                    success = true,
                    message = "Cash-out request created successfully",
                    transactionRef = cashout.TransactionRef,
                    newBalance = user.Balance.ToString("F2"),
                    Data = dto
                };

            }

            catch (Exception e)
            {

                throw;
            }

           
        }

        async Task<CashOutTransactionResponse> ICashOutService.RespondeToRequestAsync(Guid agentId, RespondeCashoutDto responseCashout)
        {
            try
            {

                var cashout = await _context.cashoutrequest.Include(c => c.User).FirstOrDefaultAsync(c =>
                                                                     c.Id == responseCashout.RequestId &&
                                                                     c.AgentId == agentId &&
                                                                     c.Status == CashoutStatus.Pending); // or == 0



                if (cashout == null)
                {
                    return new CashOutTransactionResponse
                    {
                        success = false,
                        message = "Cash-out request not found"
                    };
                }

                if (cashout.Status != CashoutStatus.Pending)
                {
                    return new CashOutTransactionResponse
                    {
                        success = false,
                        message = "Cash-out request already responded to"
                    };
                }

                cashout.RespondedAt = DateTime.UtcNow;

                if (responseCashout.IsApproved)
                {
                    // accept request agent gives cash to user
                    cashout.Status = CashoutStatus.Approved;
                    await _context.SaveChangesAsync();

                    return new CashOutTransactionResponse
                    {
                        success = true,
                        message = "Cash-out request approved",
                        transactionRef = cashout.TransactionRef,
                        Data = MapToDto(cashout, cashout.User, null)
                    };

                }
                else
                {
                    // reject request refund user
                    cashout.Status = CashoutStatus.Rejected;
                    cashout.RejectionMessage = responseCashout.RejectionMessage;
                    cashout.User.Balance += cashout.Amount + cashout.fee;


                    await _context.SaveChangesAsync();

                    return new CashOutTransactionResponse
                    {
                        success = true,
                        message = "Cash-out request rejected and amount refunded to user",
                        transactionRef = cashout.TransactionRef,
                        Data = MapToDto(cashout, cashout.User, null)
                    };
                }
            }
            catch (Exception e)
            {

                return new CashOutTransactionResponse
                {
                    success = false,
                    message = "An error occurred while responding to the cash-out request"
                };
            }
        }

        async Task<CashOutTransactionResponse> ICashOutService.CompleteRequestAsync(Guid agentId, Guid cashoutRequestId)
        {
            await using var tx = await _context.Database.BeginTransactionAsync();
            try
            {
                var cashout = await _context.cashoutrequest.FirstOrDefaultAsync
                    (c => c.Id == cashoutRequestId && c.AgentId == agentId);

                var agent = await _context.Agents.Include(a => a.User)
                   .FirstOrDefaultAsync(a => a.Id == agentId);

                if (cashout == null)
                {
                    return new CashOutTransactionResponse
                    {
                        success = false,
                        message = "Cash-out request not found"
                    };
                }

                if (cashout.Status != CashoutStatus.Approved)
                {
                    return new CashOutTransactionResponse
                    {
                        success = false,
                        message = "Cash-out request is not approved yet"
                    };
                }

               

                if (agent == null)
                    {
                    return new CashOutTransactionResponse
                    {
                        success = false,
                        message = "Agent not found"
                    };
                }

                if (agent.FloatBalance < cashout.Amount)
                {
                    return new CashOutTransactionResponse
                    {
                        success = false,
                        message = "Agent has insufficient float balance to complete the cash-out"
                    };
                }

                agent.FloatBalance -= cashout.Amount;

                //agent commission
                var commissioinStructure = 0.70m; // 70% of fee goes to agent as commission
                var agentCommission = Math.Round(cashout.fee * commissioinStructure,2);

                agent.CommissionBalance += agentCommission;

                //platform commission (todo: implement commission wallet and balance)



                cashout.Status = CashoutStatus.Completed;

                //create transaction recored

                var transaction = new Transaction
                {
                    Id = Guid.NewGuid(),
                    TransactionRef = cashout.TransactionRef,
                    Type = "Cash_out",
                    SenderId = cashout.UserId,
                    ReceiverId = null,
                    AgentId = agent.Id,
                    Amount = cashout.Amount,
                    Fee = cashout.fee,
                    Status = "Completed",
                    CreatedAt = DateTime.UtcNow
                };

                _context.Transactions.Add(transaction);
                await _context.SaveChangesAsync(); 
                await tx.CommitAsync();

                await _notificationHub.Clients.Group($"USER_{cashout.UserId}")
                    .SendAsync("cashoutCompleated",new
                    {
                       transactionRef = cashout.TransactionRef,
                       amount = cashout.Amount,
                       fee = cashout.fee,
                       status = "Completed",
                       compleatedAt = DateTime.UtcNow
                    });

                return new CashOutTransactionResponse 
                { 
                    success = true, 
                    message = $"Cash-out request completed successfully. Commission: {agentCommission:F2}",
                    transactionRef = cashout.TransactionRef,
                    Data = MapToDto(cashout, null, agent.User) 
                };

            }
            catch (Exception e)
            {

                return new CashOutTransactionResponse
                    {
                    success = false,
                    message = "An error occurred while completing the cash-out request"
                };
            }
        }



        async Task<List<CashoutResponseDto>> ICashOutService.GetAgentHistoryAsync(Guid AgentId)
        {
            return await _context.cashoutrequest
                .Include(c => c.User)
                .Where(c => c.AgentId == AgentId)
                .OrderByDescending(c => c.RequestedAt)
                .Select(c => MapToDto(c, c.User, null))
                .ToListAsync();
        }

        async Task<List<CashoutResponseDto>> ICashOutService.GetPendingForAgentAsync(Guid agentId)
        {
            return await _context.cashoutrequest
                .Where(c =>
         c.AgentId == agentId &&
         c.Status == CashoutStatus.Pending)
             .OrderByDescending(c => c.RequestedAt)
                .Select(c => new CashoutResponseDto
                {
         Id = c.Id,
         UserId = c.UserId,
         UserName = c.User.FullName,
         UserPhone = c.User.PhoneNumber,
         AgentCode = c.AgentCode,
         Amount = c.Amount,
         Status = c.Status,
         RequestedAt = c.RequestedAt,
         RespondedAt = c.RespondedAt,
         RejectionReason = c.RejectionMessage,
         TransactionRef = c.TransactionRef
            })
            .ToListAsync();
        }
        public async Task<List<CashoutResponseDto>> GetApprovedForAgentAsync(Guid UserId)
        {
            return await _context.cashoutrequest
            .AsNoTracking()
            .Where(c => c.AgentId == UserId && c.Status == CashoutStatus.Approved)
            .OrderByDescending(c => c.RespondedAt ?? c.RequestedAt)
            .Select(c => new CashoutResponseDto
            {
                Id = c.Id,
                UserId = c.UserId,
                UserName = c.User.FullName,
                UserPhone = c.User.PhoneNumber,
                AgentCode = c.AgentCode,
                Amount = c.Amount,
                Status = c.Status,
                RequestedAt = c.RequestedAt,
                RespondedAt = c.RespondedAt,
                RejectionReason = c.RejectionMessage,
                TransactionRef = c.TransactionRef
            })
            .ToListAsync();
        }

        async Task<List<CashoutResponseDto>> ICashOutService.GetUserHistoryAsync(Guid UserId)
        {
            return await _context.cashoutrequest
                 .Include(c => c.Agent)
                 .ThenInclude(a => a!.User)
                 .Where(c => c.UserId == UserId)
                 .OrderByDescending(c => c.RequestedAt)
                 .Select(c => MapToDto(c, null, c.Agent!.User))
                 .ToListAsync();
        }


        private CashoutResponseDto MapToDto(CashoutRequest cashOut, User? user, User? agentUser)
        {
            return new CashoutResponseDto
            {
                Id = cashOut.Id,
                UserId = cashOut.UserId,
                UserName = user?.FullName ?? cashOut.User?.FullName ?? string.Empty,
                UserPhone = user?.PhoneNumber ?? cashOut.User?.PhoneNumber ?? string.Empty,
                AgentCode = cashOut.AgentCode,
                AgentName = agentUser?.FullName ?? cashOut.Agent?.User?.FullName,
                Amount = cashOut.Amount,
                fee = cashOut.fee,
                Status = cashOut.Status,
                RequestedAt = cashOut.RequestedAt,
                RespondedAt = cashOut.RespondedAt,
                RejectionReason = cashOut.RejectionMessage,
                TransactionRef = cashOut.TransactionRef
            };
        }
        private decimal CalculateFee(decimal amount, Fee? feeStructure)
        {
            if (feeStructure == null)
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
