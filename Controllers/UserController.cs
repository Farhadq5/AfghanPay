using AfghanPay.API.Data;
using AfghanPay.API.DTOs;
using AfghanPay.API.Models;
using AfghanPay.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AfghanPay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly ICashOutService _cashOutService;
        private readonly ILogger<UserController> _logger;
        private readonly AppDbContext _dbContext;

        public UserController(ITransactionService transactionService, ILogger<UserController> logger,
            AppDbContext dbContext, ICashOutService cashout)
        {
            _transactionService = transactionService;
            _logger = logger;
            _dbContext = dbContext;
            _cashOutService = cashout;
        }

        /// <summary>
        /// Get current user balance
        /// </summary>
        /// <returns>Current balance</returns>

        [Authorize]
        [HttpGet("balance")]
        [ProducesResponseType(typeof(object), 200)]
        public async Task<IActionResult> GetBalance()
        {
            try
            {
                var userId = GetCurrentUserId();

                var user = await _dbContext.Users
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null)
                    return NotFound(new { message = "User not found" });

                return Ok(new
                {
                    balance = user.Balance,
                    phoneNumber = user.PhoneNumber,
                    fullName = user.FullName
                });
            }
            catch (UnauthorizedAccessException)
            {
                return Unauthorized(new { message = "Invalid or missing token" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting balance");
                return StatusCode(500, new { message = "Error retrieving balance" });
            }
        }


        /// <summary>
        /// Transfer money to another user (P2P)
        /// </summary>
        /// <param name="request">Receiver phone, amount, PIN</param>
        /// <returns>Transaction result</returns>

        [HttpPost("transfer")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            try
            {
                var senderId = GetCurrentUserId();

                _logger.LogInformation("P2P transfer initiated by User: {SenderId} to" +
                    " Receiver: {ReceiverPhone} Amount: {Amount}",
                    senderId, request.receiverPhone, request.amount);

                var response = await _transactionService.ProcessP2PTransferAsync
                    (
                        senderId,
                        request.receiverPhone,
                        request.amount,
                        request.pin
                    );
                if (!response.Success)
                {
                    _logger.LogWarning("P2P transfer failed for User: {SenderId}. Reason: {Reason}",
                        senderId, response.Message);
                    return BadRequest(response);
                }

                _logger.LogInformation("P2P transfer successful for User: {SenderId}. TransactionRef: {TransactionRef}",
                    senderId, response.TransactionRef);

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during P2P transfer");
                return StatusCode(500, new TransactionResponse
                {
                    Success = false,
                    Message = "An error occurred during the transfer"
                });
            }
        }


        /// <summary>
        /// Create a cash-out request to withdraw money from an agent
        /// </summary>
        [HttpPost("cashout")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        public async Task<IActionResult> Cashout([FromBody] CreateCashOutDto request)
        {
            var user = GetCurrentUserId();
            var result = await _cashOutService.CreateCashOutRequestAsync(user, request);

            if (!result.success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }

        /// <summary>
        /// Get transaction history
        /// </summary>
        /// <param name="limit">Number of transactions to return</param>
        /// <returns>List of transactions</returns>

        [HttpGet("transactions")]
        [ProducesResponseType(typeof(List<object>), 200)]
        public async Task<IActionResult> GetTransactions([FromQuery] int limit = 50)
        {
            try
            {
                var userId = GetCurrentUserId();
                var transactions = await _transactionService.GetUserTransactionsAsync(userId, limit);

                var response = transactions.Select(t => new
                {
                    t.Id,
                    t.Type,
                    t.Amount,
                    t.Fee,
                    Total = t.Amount + t.Fee,
                    t.TransactionRef,
                    t.Status,
                    t.CreatedAt,
                    IsSent = t.SenderId == userId,
                    IsReceived = t.ReceiverId == userId,
                }).ToList();

                return Ok(response);
            }
            catch ( Exception ex)
            {
                _logger.LogError(ex, "Error fetching transaction history");

                return StatusCode(500, "An error occurred while fetching transactions");
            }
        }

        /// <summary>
        /// Get transaction details by reference
        /// </summary>
        /// <param name="transactionRef">Transaction reference number</param>
        /// <returns>Transaction details</returns>
        
        [HttpGet("transaction/{transactionRef}")]
        [ProducesResponseType(typeof(object), 200)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetTransactionDetails(string transactionRef)
        {
            try
            {
                var userId = GetCurrentUserId();
                var transactions = await _transactionService.GetUserTransactionsAsync(userId, 100);
                var transaction = transactions.FirstOrDefault(t => t.TransactionRef == transactionRef);
                if (transaction == null)
                    return NotFound("Transaction not found");

                var response = new
                {
                    transaction.Id,
                    transaction.Type,
                    transaction.Amount,
                    transaction.Fee,
                    Total = transaction.Amount + transaction.Fee,
                    transaction.Status,
                    transaction.CreatedAt,
                    IsSent = transaction.SenderId == userId,
                    IsReceived = transaction.ReceiverId == userId,
                };
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching transaction details");
                return StatusCode(500, "An error occurred while fetching transaction details");
            }
        }


        /// <summary>
        /// Get user's cash-out history
        /// </summary>
        [HttpGet("cashout/history")]
        [ProducesResponseType(typeof(List<object>), 200)]
        public async Task<IActionResult> GetCashOutHistory()
        {
            var userId = GetCurrentUserId();
            var history = await _cashOutService.GetUserHistoryAsync(userId);
            return Ok(history);
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return Guid.Parse(userIdClaim!);
        }

        private Guid GetCurrentUserId()
        {
          var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if(string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User ID not found");

            return Guid.Parse(userIdClaim);
        }
    }
}
