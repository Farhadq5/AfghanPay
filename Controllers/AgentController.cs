using AfghanPay.API.Data;
using AfghanPay.API.DTOs;
using AfghanPay.API.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AfghanPay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AgentController : ControllerBase
    {
        private readonly ILogger<AgentController> _logger;
        private readonly ITransactionService _transactionService;
        private readonly ICashOutService _cashOutService;
        private readonly IAgentService _agentService;
        private readonly AppDbContext _dbContext;

        public AgentController(ILogger<AgentController> logger,
            ITransactionService transactionService,
            IAgentService agentService,
            AppDbContext dbContext,
            ICashOutService cashOutService)
        {
            _logger = logger;
            _transactionService = transactionService;
            _agentService = agentService;
            _dbContext = dbContext;
            _cashOutService = cashOutService;
        }

        /// <summary>
        /// Get agent dashboard data
        /// </summary>
        /// <returns>Float balance, commissions, today's stats</returns>\

        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(AgentDashboardDto), 200)]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var agentId = await GetCurrentAgentIdAsync();
                var dashboardDto = await _agentService.GetDashboardDtoAsync(agentId);
                return Ok(dashboardDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching agent dashboard");
                return StatusCode(500, "An error occurred while fetching dashboard data");
            }
        }

        /// <summary>
        /// Process cash-in for a customer
        /// </summary>
        /// <param name="request">Customer phone and amount</param>
        /// <returns>Transaction result</returns>
        
        [HttpPost("cash-in")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CashIn([FromBody] CashInRequest request)
        {
            try
            {
                var agentId = await GetCurrentAgentIdAsync();

                _logger.LogInformation("Processing cash-in: AgentId={AgentId}, CustomerPhone={CustomerPhone}, Amount={Amount}",
                    agentId, request.CustomerPhoneNumber, request.amount);

                var response = await _transactionService.ProcessCashInAsync(
                    agentId, request.CustomerPhoneNumber, request.amount);

                if (!response.Success)
                {
                    _logger.LogWarning("Cash-in failed: {Reason}", response.Message);
                    return BadRequest(response);
                }

                _logger.LogInformation("Cash-in successful: {TransactionId}", response.TransactionRef);
                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cash-in");
                return StatusCode(500, new TransactionResponse
                {
                    Success = false,
                    Message = "An error occurred during cash-in"
                });
            }
        }

        /// <summary>
        /// Process cash-out for a customer
        /// </summary>
        /// <param name="request">Customer phone and amount</param>
        /// <returns>Transaction result</returns>
        
        [HttpPost("cash-out/respond")]
        [ProducesResponseType(typeof(TransactionResponse), 200)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> CashOut([FromBody] RespondeCashoutDto request)
        {
            try
            {
                var agentId = await GetCurrentAgentIdAsync();

                _logger.LogInformation("Processing cash-out: AgentId={AgentId}",
                    agentId);

                var response = await _cashOutService.RespondeToRequestAsync(
                    agentId, request);

                if (!response.success)
                {
                    _logger.LogWarning("Cash-out approval  failed: {Reason}", response.message);
                    return BadRequest(response);
                }

                _logger.LogInformation("Cash-out successful: {TransactionId}", response.transactionRef);
                return Ok(response);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing cash-out");
                return StatusCode(500, new TransactionResponse
                {
                    Success = false,
                    Message = "An error occurred during cash-out"
                });
            }
        }

        /// <summary>
        /// Mark a cash-out as completed (after giving cash to user)
        /// </summary>
        [HttpPost("cash-out/complete/{cashOutId}")]
        [ProducesResponseType(typeof(bool), 200)]
        public async Task<IActionResult> CompleteCashOut([FromRoute] Guid cashOutId)
        {

            try
            {
                var agentId = await GetCurrentAgentIdAsync();
                _logger.LogInformation("Completing cash-out: AgentId={AgentId}, CashOutId={CashOutId}",
                    agentId, cashOutId);
                var result = await _cashOutService.CompleteRequestAsync(agentId, cashOutId);
                if (!result.success)
                {
                    _logger.LogWarning("Cash-out completion failed for CashOutId={CashOutId}", cashOutId);
                    return BadRequest(false);
                }
                _logger.LogInformation("Cash-out completed successfully for CashOutId={CashOutId}", cashOutId);
                return Ok(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing cash-out");
                return StatusCode(500, false);
            }
        }

        [HttpGet("cash-out/pending")]
        [ProducesResponseType(typeof(List<CashoutResponseDto>), 200)]
        public async Task<IActionResult> GetPendingCashOuts()
        {
            try
            {
                var agentId = await GetCurrentAgentIdAsync();
                var pendingRequests = await _cashOutService.GetPendingForAgentAsync(agentId);
                return Ok(pendingRequests);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching pending cash-out requests");
                return StatusCode(500, "An error occurred while fetching pending requests");
            }
        }

        [HttpGet("cash-out/approved")]
        [ProducesResponseType(typeof(List<CashoutResponseDto>), 200)]
        public async Task<IActionResult> GetApprovedCashOuts()
        {
            try
            {
                var agentId = await GetCurrentAgentIdAsync();
                var approvedRequests = await _cashOutService.GetApprovedForAgentAsync(agentId);
                return Ok(approvedRequests);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error fetching pending cash-out requests");
                return StatusCode(500, "An error occurred while fetching pending requests");

            }
        }

        /// <summary>
        /// Get agent transaction history
        /// </summary>
        /// <param name="limit">Number of transactions to return</param>
        /// <returns>List of transactions</returns>

        [HttpGet("transactions")]
        [ProducesResponseType(typeof(List<Object>), 200)]
        public async Task<IActionResult> GetTransactions([FromQuery] int limit = 50)
        {
            try
            {
                var agentId = await GetCurrentAgentIdAsync();

                var transactions = await _transactionService.GetAgentTransactionsAsync(agentId, limit);

                var result = transactions.Select(t => new
                {
                  t.Id,
                  t.TransactionRef,
                  t.Type,
                  t.Amount,
                  t.Fee,
                  t.Status,
                  t.CreatedAt
                }).ToList();
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching agent transactions");
                return StatusCode(500, "An error occurred while fetching transactions");
            }
        }

        private async Task<Guid> GetCurrentAgentIdAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(userIdClaim))
            {
                throw new Exception("User ID claim is missing");
            }

            var userId = Guid.Parse(userIdClaim);

            var agentid = await _dbContext.Agents.Where(a => a.UserId == userId)
                .Select(a => a.Id)
                .FirstOrDefaultAsync();
            if (agentid == Guid.Empty)
            {
                throw new Exception("Agent not found for the current user");
            }

            return agentid;
        }
    }
}
