using AfghanPay.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace AfghanPay.API.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        private readonly ILogger<NotificationHub> _logger;

        public NotificationHub(ILogger<NotificationHub> logger)
        {
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                      ?? Context.User?.FindFirst("sub")?.Value;

            if (string.IsNullOrWhiteSpace(userId))
            {
                _logger.LogWarning("❌ Connected without user id claim. Aborting.");
                Context.Abort();
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, $"USER_{userId}");
            _logger.LogInformation("✅ User joined group USER_{UserId}", userId);

            await base.OnConnectedAsync();
        }

    }

}
