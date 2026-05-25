using LiteQMS.Data;
using LiteQMS.Models;
using Microsoft.EntityFrameworkCore;

namespace LiteQMS.Services;

public class CallService
{
    private readonly AppDbContext _db;
    private readonly QueueStateService _queueState;
    private readonly ILogger<CallService> _logger;

    public CallService(AppDbContext db, QueueStateService queueState, ILogger<CallService> logger)
    {
        _db = db;
        _queueState = queueState;
        _logger = logger;
    }

    public async Task<CallResult> CallPatientAsync(string patientNumber, string roomNumber)
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
                .AsNoTracking()
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

                _logger.LogInformation("Patient {PatientNumber} called from {RoomNumber} (display updated)", patientNumber, roomNumber);
            }
            else
            {
                _logger.LogInformation("Patient {PatientNumber} called from {RoomNumber} (same as current, display unchanged)", patientNumber, roomNumber);
            }

            return new CallResult(true, string.Empty, newCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "CallPatient failed for {PatientNumber} in {RoomNumber}", patientNumber, roomNumber);
            return new CallResult(false, "System error. Please try again.", 0);
        }
    }
}
