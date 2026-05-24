using System.Diagnostics;
using System.Windows.Forms;
using LiteQMS.Data;
using LiteQMS.Hubs;
using LiteQMS.Services;
using Microsoft.EntityFrameworkCore;

namespace LiteQMS;

static class Program
{
    [STAThread]
    static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddRazorPages();
        builder.Services.AddSignalR();
        builder.Services.AddDistributedMemoryCache();
        builder.Services.AddSession(options =>
        {
            options.IdleTimeout = TimeSpan.FromHours(8);
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
        });

        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
            ?? "Data Source=LiteQMS.db";

        if (!builder.Environment.IsDevelopment() && connectionString.Contains("LiteQMS.db"))
        {
            var dbFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LiteQMS");
            Directory.CreateDirectory(dbFolder);
            connectionString = $"Data Source={Path.Combine(dbFolder, "LiteQMS.db")}";
        }

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString, sqlOptions =>
            {
                sqlOptions.CommandTimeout(30);
            }));

        builder.Services.AddSingleton<QueueStateService>();
        builder.Services.AddHostedService<MidnightResetService>();

        var app = builder.Build();

        using (var scope = app.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            db.Database.EnsureCreated();
        }

        app.UseExceptionHandler("/Error");

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseRouting();
        app.UseSession();
        app.UseAuthorization();

        app.MapStaticAssets();
        app.MapRazorPages()
           .WithStaticAssets();

        app.MapHub<QueueHub>("/queueHub");

        await app.StartAsync();

        if (!app.Environment.IsDevelopment())
        {
            var url = app.Urls.FirstOrDefault() ?? "http://localhost:5000";
            OpenBrowser(url);

            try
            {
                using var icon = new NotifyIcon
                {
                    Icon = SystemIcons.Application,
                    Text = "LiteQMS — Queue Calling System",
                    Visible = true
                };

                var menu = new ContextMenuStrip();
                menu.Items.Add("Open LiteQMS", null, (_, _) => OpenBrowser(url));
                menu.Items.Add(new ToolStripSeparator());
                menu.Items.Add("Quit", null, async (_, _) =>
                {
                    try
                    {
                        icon.Visible = false;
                        await app.StopAsync();
                    }
                    catch
                    {
                        // Silent — shutdown is non-critical
                    }
                    Application.ExitThread();
                });

                icon.ContextMenuStrip = menu;
                icon.DoubleClick += (_, _) => OpenBrowser(url);

                icon.ShowBalloonTip(3000, "LiteQMS", "Server is running", ToolTipIcon.Info);

                Application.Run();
            }
            catch
            {
                // Tray not available (e.g. no GUI) — keep server running
                await app.WaitForShutdownAsync();
            }
        }
        else
        {
            await app.WaitForShutdownAsync();
        }
    }

    static void OpenBrowser(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        catch
        {
            // Silent — browser launch is non-critical
        }
    }
}
