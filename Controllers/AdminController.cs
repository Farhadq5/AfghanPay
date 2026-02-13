using AfghanPay.API.DTOs;
using AfghanPay.API.Models;
using AfghanPay.API.Services.Interfaces;
using AfghanPay.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AfghanPay.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "admin,super_admin")]
    public class AdminController : ControllerBase
    {
        private readonly ILogger<AdminController> _logger;
        private readonly IAdminService _adminService;

        public AdminController(ILogger<AdminController> logger,
            IAdminService adminService)
        {
            _logger = logger;
            _adminService = adminService;
        }

        /// <summary>
        /// Get today's dashboard statistics
        /// </summary>
        /// <returns>Transaction counts, volumes, fees, commissions, profit</returns>
        [HttpGet("dashboard")]
        [ProducesResponseType(typeof(AdminDashboardDto), 200)]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                var dashboard = await _adminService.GetDashboardAsync();
                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin dashboard");
                return StatusCode(500, new { message = "Error retrieving dashboard" });
            }
        }

        /// <summary>
        /// Get all transactions (paginated)
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>List of transactions</returns>
        [HttpGet("transactions")]
        [ProducesResponseType(typeof(List<Transaction>), 200)]
        public async Task<IActionResult> GetTransactions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                var transactions = await _adminService.GetAllTransactionsAsync(page, pageSize);

                var result = transactions.Select(t => new
                {
                    t.Id,
                    t.TransactionRef,
                    t.Type,
                    t.Amount,
                    t.Fee,
                    Total = t.Amount + t.Fee,
                    t.Status,
                    t.CreatedAt,
                    t.SenderId,
                    t.ReceiverId,
                    t.AgentId
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions");
                return StatusCode(500, new { message = "Error retrieving transactions" });
            }
        }

        /// <summary>
        /// Get daily report for last N days
        /// </summary>
        /// <param name="days">Number of days (default 7)</param>
        /// <returns>Daily statistics</returns>
        [HttpGet("daily-report")]
        [ProducesResponseType(typeof(List<DailyReportDto>), 200)]
        public async Task<IActionResult> GetDailyReport([FromQuery] int days = 7)
        {
            try
            {
                var report = await _adminService.GetDailyReportAsync(days);
                return Ok(report);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting daily report");
                return StatusCode(500, new { message = "Error retrieving report" });
            }
        }

        /// <summary>
        /// Get all users
        /// </summary>
        /// <returns>List of all users</returns>
        [HttpGet("users")]
        [ProducesResponseType(typeof(List<User>), 200)]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var users = await _adminService.GetAllUsersAsync();

                var result = users.Select(u => new
                {
                    u.Id,
                    u.PhoneNumber,
                    u.FullName,
                    u.Balance,
                    u.Status,
                    u.CreatedAt
                });

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting users");
                return StatusCode(500, new { message = "Error retrieving users" });
            }
        }

        /// <summary>
        /// Get all agents
        /// </summary>
        /// <returns>List of all agents</returns>
        [HttpGet("agents")]
        [ProducesResponseType(typeof(List<AgentListDto>), 200)]
        public async Task<IActionResult> GetAgents()
        {
            try
            {
                var agents = await _adminService.GetAllAgentsAsync();
                return Ok(agents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting agents");
                return StatusCode(500, new { message = "Error retrieving agents" });
            }
        }

        /// <summary>
        /// Get admin activity events (paginated)
        /// </summary>
        /// <param name="page">Page number</param>
        /// <param name="pageSize">Items per page</param>
        /// <returns>List of admin events</returns>
        [HttpGet("events")]
        [ProducesResponseType(typeof(List<AdminEvents>), 200)]
        public async Task<IActionResult> GetAdminEvents(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            try
            {
                var events = await _adminService.GetAdminEventsAsync(page, pageSize);
                return Ok(events);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting admin events");
                return StatusCode(500, new { message = "Error retrieving admin events" });
            }
        }
    }
}
