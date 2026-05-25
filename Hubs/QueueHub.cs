using LiteQMS.Data;
using LiteQMS.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LiteQMS.Hubs;

public class QueueHub : Hub
{
    private readonly QueueStateService _queueState;
    private readonly AppDbContext _db;
    private readonly ILogger<QueueHub> _logger;

    public QueueHub(QueueStateService queueState, AppDbContext db, ILogger<QueueHub> logger)
    {
        _queueState = queueState;
        _db = db;
        _logger = logger;
    }

    public async Task<CallResult> CallPatient(string patientNumber, string roomNumber)
    {
        patientNumber = patientNumber.Trim();
        roomNumber = roomNumber.Trim();

        if (string.IsNullOrWhiteSpace(patientNumber) || patientNumber.Length != 4 || !patientNumber.All(char.IsDigit))
        {
            return new CallResult(false, "Must be 4 digits", 0);
        }

        if (string.IsNullOrWhiteSpace(roomNumber) || roomNumber.Length > 50 || !roomNumber.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '/'))
        {
            return new CallResult(false, "Invalid room number", 0);
        }

        try
        {
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "CallPatient failed for {PatientNumber} in {RoomNumber}", patientNumber, roomNumber);
            return new CallResult(false, "System error. Please try again.", 0);
        }
    }

    public async Task ToggleCNA(int callRecordId)
    {
        try
        {
            var record = await _db.CallRecords.FindAsync(callRecordId);
            if (record != null)
            {
                record.IsCNA = !record.IsCNA;
                await _db.SaveChangesAsync();
                await Clients.All.SendAsync("CNAUpdated", record.Id, record.IsCNA);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "ToggleCNA failed for record {RecordId}", callRecordId);
        }
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
