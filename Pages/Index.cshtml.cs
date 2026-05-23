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
        RoomNumber = RoomNumber.Trim();

        if (string.IsNullOrWhiteSpace(RoomNumber))
        {
            ModelState.AddModelError("RoomNumber", "Room number is required");
            return Page();
        }

        if (RoomNumber.Length > 50 || !RoomNumber.All(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-' || c == '/'))
        {
            ModelState.AddModelError("RoomNumber", "Only letters, digits, spaces, hyphens and slashes allowed");
            return Page();
        }

        HttpContext.Session.SetString("RoomNumber", RoomNumber.ToUpper());
        return RedirectToPage("/CallPanel");
    }
}
