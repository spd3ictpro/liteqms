using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace LiteQMS.Pages;

public class IndexModel : PageModel
{
    [BindProperty]
    public string RoomNumber { get; set; } = string.Empty;

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        if (string.IsNullOrWhiteSpace(RoomNumber))
        {
            ModelState.AddModelError("RoomNumber", "Room number is required");
            return Page();
        }

        HttpContext.Session.SetString("RoomNumber", RoomNumber.Trim().ToUpper());
        return RedirectToPage("/CallPanel");
    }
}
