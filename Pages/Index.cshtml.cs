using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QRCoder;

namespace LiteQMS.Pages;

public class IndexModel : PageModel
{
    private readonly IConfiguration _config;

    public IndexModel(IConfiguration config)
    {
        _config = config;
    }

    [BindProperty]
    public string RoomNumber { get; set; } = string.Empty;

    public string HostnameUrl { get; set; } = string.Empty;

    public string QrCodeBase64 { get; set; } = string.Empty;

    public void OnGet()
    {
        var port = _config["LiteQMS:Port"] ?? "5000";
        HostnameUrl = $"http://{Environment.MachineName}:{port}";

        using var generator = new QRCodeGenerator();
        var qrData = generator.CreateQrCode(HostnameUrl, QRCodeGenerator.ECCLevel.Q);
        using var png = new PngByteQRCode(qrData);
        QrCodeBase64 = Convert.ToBase64String(png.GetGraphic(4));
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
