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
        var query = _db.CallRecords.AsNoTracking();

        var from = DateTime.Today;
        var to = DateTime.Today;

        if (!string.IsNullOrEmpty(DateFrom)) DateTime.TryParse(DateFrom, out from);
        if (!string.IsNullOrEmpty(DateTo)) DateTime.TryParse(DateTo, out to);

        if (from > to) from = to;

        var maxFrom = to.AddDays(-90);
        if (from < maxFrom) from = maxFrom;

        query = query.Where(r => r.Timestamp >= from && r.Timestamp <= to.AddDays(1).AddTicks(-1));

        Records = await query
            .OrderByDescending(r => r.Timestamp)
            .Take(500)
            .ToListAsync();

        DateFrom = from.ToString("yyyy-MM-dd");
        DateTo = to.ToString("yyyy-MM-dd");
    }
}
