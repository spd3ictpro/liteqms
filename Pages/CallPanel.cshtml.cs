using LiteQMS.Data;
using LiteQMS.Models;
using LiteQMS.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LiteQMS.Pages;

public class CallPanelModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly QueueStateService _queueState;
    private readonly CallService _callService;
    private readonly ILogger<CallPanelModel> _logger;

    public CallPanelModel(AppDbContext db, QueueStateService queueState, CallService callService, ILogger<CallPanelModel> logger)
    {
        _db = db;
        _queueState = queueState;
        _callService = callService;
        _logger = logger;
    }

    [BindProperty]
    public string PatientNumber { get; set; } = string.Empty;

    public string RoomNumber { get; set; } = string.Empty;
    public List<CallRecord> RecentCalls { get; set; } = new();
    public List<RecentCall> PreviewRecentCalls { get; set; } = new();
    public int CallCount { get; set; }
    public string? PreviewPatientNumber { get; set; }
    public string? PreviewRoomNumber { get; set; }

    public IActionResult OnGet()
    {
        RoomNumber = HttpContext.Session.GetString("RoomNumber") ?? string.Empty;
        if (string.IsNullOrEmpty(RoomNumber))
        {
            return RedirectToPage("/Index");
        }

        LoadRecentCalls();

        var state = _queueState.CurrentState;
        if (state != null)
        {
            PreviewPatientNumber = state.PatientNumber;
            PreviewRoomNumber = state.RoomNumber;
            PreviewRecentCalls = state.RecentCalls;
        }

        return Page();
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

        var isValidFormat = PatientNumber.Length == 4 && PatientNumber.All(char.IsDigit);

        if (!isValidFormat)
        {
            ModelState.AddModelError("PatientNumber", "Must be 4 digits");
            LoadRecentCalls();
            return Page();
        }

        var result = await _callService.CallPatientAsync(PatientNumber, RoomNumber);

        if (result.Success)
        {
            CallCount = result.CallCount;
            return RedirectToPage();
        }

        ModelState.AddModelError(string.Empty, result.Error);
        LoadRecentCalls();
        return Page();
    }

    private void LoadRecentCalls()
    {
        var today = DateTime.Today;
        var room = RoomNumber;
        RecentCalls = _db.CallRecords
            .AsNoTracking()
            .Where(r => r.Timestamp >= today && r.RoomNumber == room)
            .OrderByDescending(r => r.Timestamp)
            .Take(10)
            .ToList();
    }
}
