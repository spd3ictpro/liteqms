using LiteQMS.Services;
using Microsoft.AspNetCore.SignalR;

namespace LiteQMS.Hubs;

public class QueueHub : Hub
{
    private readonly QueueStateService _queueState;

    public QueueHub(QueueStateService queueState)
    {
        _queueState = queueState;
    }

    public async Task BroadcastNewCall(CallState state)
    {
        await Clients.All.SendAsync("NewCall", state);
    }

    public async Task BroadcastCNAUpdate(int callRecordId, bool isCNA)
    {
        await Clients.All.SendAsync("CNAUpdated", callRecordId, isCNA);
    }

    public async Task BroadcastQueueReset()
    {
        await Clients.All.SendAsync("QueueReset");
    }

    public async Task RequestCurrentState()
    {
        var state = _queueState.CurrentState;
        if (state != null)
        {
            await Clients.Caller.SendAsync("ReceiveCurrentState", state);
        }
    }
}

public record CallState(
    string RoomNumber,
    string PatientNumber,
    DateTime Timestamp,
    List<RecentCall> RecentCalls
);

public record RecentCall(
    int Id,
    string RoomNumber,
    string PatientNumber,
    DateTime Timestamp,
    bool IsCNA
);
