using LiteQMS.Data;
using LiteQMS.Hubs;
using LiteQMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace LiteQMS.Pages;

public class DoctorModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly QueueStateService _queueState;
    private readonly IHubContext<QueueHub> _hubContext;
    private readonly ILogger<DoctorModel> _logger;

    public DoctorModel(AppDbContext db, QueueStateService queueState, IHubContext<QueueHub> hubContext, ILogger<DoctorModel> logger)
    {
        _db = db;
        _queueState = queueState;
        _hubContext = hubContext;
        _logger = logger;
    }

    [BindProperty]
    public string PatientNumber { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;
    public List<CallRecord> RecentCalls { get; set; } = new();
    public bool IsDuplicate { get; set; }
    public bool ShowFormatWarning { get; set; }

    public void OnGet()
    {
        RoomNumber = HttpContext.Session.GetString("RoomNumber") ?? string.Empty;
        if (string.IsNullOrEmpty(RoomNumber))
        {
            RedirectToPage("/Index");
            return;
        }

        LoadRecentCalls();
    }

    public async Task<IActionResult> OnPost()
    {
        RoomNumber = HttpContext.Session.GetString("RoomNumber") ?? string.Empty;
        if (string.IsNullOrEmpty(RoomNumber))
        {
            return RedirectToPage("/Index");
        }

        if (string.IsNullOrWhiteSpace(PatientNumber))
        {
            ModelState.AddModelError("PatientNumber", "Patient number is required");
            LoadRecentCalls();
            return Page();
        }

        PatientNumber = PatientNumber.Trim();

        var validPrefixes = new[] { "1", "3", "5", "7" };
        var isValidFormat = PatientNumber.Length == 4 && validPrefixes.Contains(PatientNumber.Substring(0, 1)) && PatientNumber.All(char.IsDigit);

        if (!isValidFormat)
        {
            ShowFormatWarning = true;
            ModelState.AddModelError("PatientNumber", "Format should be 1xxx, 3xxx, 5xxx, or 7xxx (4 digits)");
            LoadRecentCalls();
            return Page();
        }

        var today = DateTime.Today;
        var todayCalls = await _db.CallRecords
            .Where(r => r.Timestamp >= today)
            .ToListAsync();

        IsDuplicate = todayCalls.Any(r => r.PatientNumber == PatientNumber && !r.IsCNA);

        var callRecord = new CallRecord
        {
            RoomNumber = RoomNumber,
            PatientNumber = PatientNumber,
            Timestamp = DateTime.Now,
            IsCNA = false
        };

        _db.CallRecords.Add(callRecord);
        await _db.SaveChangesAsync();

        var currentState = _queueState.CurrentState;
        var isSameAsCurrent = currentState != null && currentState.PatientNumber == PatientNumber;

        if (!isSameAsCurrent)
        {
            var recentCalls = todayCalls
                .Where(r => r.PatientNumber != PatientNumber)
                .OrderByDescending(r => r.Timestamp)
                .Take(4)
                .Select(r => new RecentCall(r.Id, r.RoomNumber, r.PatientNumber, r.Timestamp, r.IsCNA))
                .ToList();

            var state = new CallState(RoomNumber, PatientNumber, callRecord.Timestamp, recentCalls);
            await _queueState.BroadcastStateAsync(state);

            _logger.LogInformation("Patient {PatientNumber} called from {RoomNumber} (display updated)", PatientNumber, RoomNumber);
        }
        else
        {
            _logger.LogInformation("Patient {PatientNumber} called from {RoomNumber} (same as current, display unchanged)", PatientNumber, RoomNumber);
        }

        PatientNumber = string.Empty;
        LoadRecentCalls();
        return Page();
    }

    public async Task<IActionResult> OnPostCNA(int id)
    {
        var record = await _db.CallRecords.FindAsync(id);
        if (record != null)
        {
            record.IsCNA = !record.IsCNA;
            await _db.SaveChangesAsync();
            await _hubContext.Clients.All.SendAsync("CNAUpdated", record.Id, record.IsCNA);
        }

        return RedirectToPage();
    }

    private void LoadRecentCalls()
    {
        var today = DateTime.Today;
        var room = RoomNumber;
        RecentCalls = _db.CallRecords
            .Where(r => r.Timestamp >= today && r.RoomNumber == room)
            .OrderByDescending(r => r.Timestamp)
            .Take(10)
            .ToList();
    }
}
