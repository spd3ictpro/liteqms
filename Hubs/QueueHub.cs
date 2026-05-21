using LiteQMS.Data;
using LiteQMS.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LiteQMS.Hubs;

public class QueueHub : Hub
{
    private readonly QueueStateService _queueState;
    private readonly AppDbContext _db;

    public QueueHub(QueueStateService queueState, AppDbContext db)
    {
        _queueState = queueState;
        _db = db;
    }

    public async Task<CallResult> CallPatient(string patientNumber, string roomNumber)
    {
        patientNumber = patientNumber.Trim();

        if (string.IsNullOrWhiteSpace(patientNumber) || patientNumber.Length != 4 || !patientNumber.All(char.IsDigit))
        {
            return new CallResult(false, "Must be 4 digits", 0);
        }

        var today = DateTime.Today;
        var todayCalls = await _db.CallRecords
            .Where(r => r.Timestamp >= today)
            .ToListAsync();

        var callCount = todayCalls.Count(r => r.PatientNumber == patientNumber && !r.IsCNA);
        var newCount = callCount + 1;

        var callRecord = new CallRecord
        {
            RoomNumber = roomNumber,
            PatientNumber = patientNumber,
            Timestamp = DateTime.Now,
            IsCNA = false
        };

        _db.CallRecords.Add(callRecord);
        await _db.SaveChangesAsync();

        var currentState = _queueState.CurrentState;
        var isSameAsCurrent = currentState != null && currentState.PatientNumber == patientNumber;
        var isRecall = callCount > 0;

        if (!isSameAsCurrent || isRecall)
        {
            var recentCalls = todayCalls
                .Where(r => r.PatientNumber != patientNumber)
                .OrderByDescending(r => r.Timestamp)
                .Take(4)
                .Select(r => new RecentCall(r.Id, r.RoomNumber, r.PatientNumber, r.Timestamp, r.IsCNA))
                .ToList();

            var state = new CallState(roomNumber, patientNumber, callRecord.Timestamp, recentCalls, newCount, isRecall);
            await _queueState.BroadcastStateAsync(state);
        }

        return new CallResult(true, string.Empty, callCount + 1);
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
    List<RecentCall> RecentCalls,
    int CallCount,
    bool IsRecall
);

public record RecentCall(
    int Id,
    string RoomNumber,
    string PatientNumber,
    DateTime Timestamp,
    bool IsCNA
);

public record CallResult(bool Success, string Error, int CallCount);
