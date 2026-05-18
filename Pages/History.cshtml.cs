using LiteQMS.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace LiteQMS.Pages;

public class HistoryModel : PageModel
{
    private readonly AppDbContext _db;

    public HistoryModel(AppDbContext db)
    {
        _db = db;
    }

    public List<CallRecord> Records { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public string? DateFrom { get; set; }

    [BindProperty(SupportsGet = true)]
    public string? DateTo { get; set; }

    public async Task OnGet()
    {
        var query = _db.CallRecords.AsQueryable();

        if (!string.IsNullOrEmpty(DateFrom) && DateTime.TryParse(DateFrom, out var from))
        {
            query = query.Where(r => r.Timestamp >= from);
        }

        if (!string.IsNullOrEmpty(DateTo) && DateTime.TryParse(DateTo, out var to))
        {
            query = query.Where(r => r.Timestamp <= to.AddDays(1).AddTicks(-1));
        }

        Records = await query
            .OrderByDescending(r => r.Timestamp)
            .Take(500)
            .ToListAsync();

        if (string.IsNullOrEmpty(DateFrom))
        {
            DateFrom = DateTime.Today.ToString("yyyy-MM-dd");
        }
        if (string.IsNullOrEmpty(DateTo))
        {
            DateTo = DateTime.Today.ToString("yyyy-MM-dd");
        }
    }
}
