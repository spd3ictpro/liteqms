using LiteQMS.Hubs;
using Microsoft.AspNetCore.SignalR;

namespace LiteQMS.Services;

public class QueueStateService
{
    private readonly IHubContext<QueueHub> _hubContext;
    private CallState? _currentState;

    public CallState? CurrentState => _currentState;

    public QueueStateService(IHubContext<QueueHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public void UpdateState(CallState state)
    {
        _currentState = state;
    }

    public async Task BroadcastStateAsync(CallState state)
    {
        _currentState = state;
        await _hubContext.Clients.All.SendAsync("NewCall", state);
    }

    public async Task ResetStateAsync()
    {
        _currentState = null;
        await _hubContext.Clients.All.SendAsync("QueueReset");
    }
}
