using Microsoft.AspNetCore.SignalR;

namespace LiteQMS.Hubs;

public class QueueHub : Hub
{
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
        await Clients.Caller.SendAsync("RequestStateSync");
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
