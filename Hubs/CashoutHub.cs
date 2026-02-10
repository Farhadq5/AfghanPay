using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

[Authorize]
public class CashoutHub : Hub
{
    private readonly ILogger<CashoutHub> _logger;

    public CashoutHub(ILogger<CashoutHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var agentCode = Context.User?.FindFirst("AgentCode")?.Value;

        if (string.IsNullOrWhiteSpace(agentCode))
        {
            _logger.LogWarning("❌ SignalR connected without JWT AgentCode");
            Context.Abort();
            return;
        }

        agentCode = agentCode.ToUpper().Trim();

        await Groups.AddToGroupAsync(Context.ConnectionId, agentCode);

        _logger.LogInformation($"✅ Agent {agentCode} joined SignalR");

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception exception)
    {
        _logger.LogInformation($"Client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }
}