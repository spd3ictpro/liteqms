using LiteQMS.Data;
using LiteQMS.Hubs;
using LiteQMS.Services;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LiteQMS.Pages;

public class DisplayModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly QueueStateService _queueState;

    public DisplayModel(AppDbContext db, QueueStateService queueState)
    {
        _db = db;
        _queueState = queueState;
    }

    public CallState? CurrentState { get; set; }

    public async Task OnGet()
    {
        var state = _queueState.CurrentState;
        if (state != null)
        {
            CurrentState = state;
        }
        else
        {
            var today = DateTime.Today;
            var latest = await _db.CallRecords
                .Where(r => r.Timestamp >= today)
                .OrderByDescending(r => r.Timestamp)
                .FirstOrDefaultAsync();

            if (latest != null)
            {
                var recentCalls = await _db.CallRecords
                    .Where(r => r.Timestamp >= today && r.Id != latest.Id)
                    .OrderByDescending(r => r.Timestamp)
                    .Take(5)
                    .Select(r => new RecentCall(r.Id, r.RoomNumber, r.PatientNumber, r.Timestamp, r.IsCNA))
                    .ToListAsync();

                var callCount = await _db.CallRecords
                    .CountAsync(r => r.Timestamp >= today && r.PatientNumber == latest.PatientNumber && !r.IsCNA);

                CurrentState = new CallState(
                    latest.RoomNumber,
                    latest.PatientNumber,
                    latest.Timestamp,
                    recentCalls,
                    callCount
                );
                _queueState.UpdateState(CurrentState);
            }
        }
    }
}
