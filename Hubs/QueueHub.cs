using LiteQMS.Data;
using LiteQMS.Models;
using LiteQMS.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LiteQMS.Hubs;

public class QueueHub : Hub
{
    private readonly QueueStateService _queueState;
    private readonly CallService _callService;
    private readonly AppDbContext _db;
    private readonly ILogger<QueueHub> _logger;

    public QueueHub(QueueStateService queueState, CallService callService, AppDbContext db, ILogger<QueueHub> logger)
    {
        _queueState = queueState;
        _callService = callService;
        _db = db;
        _logger = logger;
    }

    public async Task<CallResult> CallPatient(string patientNumber, string roomNumber)
    {
        return await _callService.CallPatientAsync(patientNumber, roomNumber);
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
