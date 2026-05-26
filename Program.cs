using System.Diagnostics;
using System.Net.NetworkInformation;
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

        var configuredPort = 5000;
        var configuredUrls = builder.Configuration["Urls"] ?? "http://0.0.0.0:5000";
        if (Uri.TryCreate(configuredUrls, UriKind.Absolute, out var uri))
        {
            configuredPort = uri.Port;
        }

        var actualPort = configuredPort;
        if (!builder.Environment.IsDevelopment())
        {
            actualPort = FindFreePort(configuredPort);
            if (actualPort != configuredPort)
            {
                builder.Configuration["Urls"] = $"http://0.0.0.0:{actualPort}";
            }
            builder.Configuration["LiteQMS:Port"] = actualPort.ToString();
        }

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
            var localUrl = $"http://localhost:{actualPort}";
            var hostnameUrl = $"http://{Environment.MachineName}:{actualPort}";

            var logPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LiteQMS", "startup.log");
            File.WriteAllText(logPath,
                $"LiteQMS started at {DateTime.Now:dd/MM/yyyy HH:mm:ss}\n" +
                $"→ This PC:      {localUrl}\n" +
                $"→ Other devices: {hostnameUrl}\n");

            OpenBrowser(localUrl);

            try
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.SetHighDpiMode(HighDpiMode.SystemAware);

                Icon trayIcon;
                try
                {
                    trayIcon = new Icon("liteqms.ico");
                }
                catch
                {
                    trayIcon = SystemIcons.Application;
                }

                using var icon = new NotifyIcon
                {
                    Icon = trayIcon,
                    Text = "LiteQMS — " + hostnameUrl,
                    Visible = true
                };

                var menu = new ContextMenuStrip();
                menu.Items.Add("Open LiteQMS", null, (_, _) => OpenBrowser(localUrl));
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
                icon.DoubleClick += (_, _) => OpenBrowser(localUrl);
                icon.BalloonTipText = $"Other devices: {hostnameUrl}";

                icon.ShowBalloonTip(3000, "LiteQMS — Server is running", icon.BalloonTipText, ToolTipIcon.Info);

                Application.Run();
            }
            catch (Exception ex)
            {
                File.AppendAllText(logPath, $"Tray icon failed: {ex.Message}\n");
                await app.WaitForShutdownAsync();
            }
        }
        else
        {
            await app.WaitForShutdownAsync();
        }
    }

    static int FindFreePort(int preferredPort)
    {
        var usedPorts = IPGlobalProperties.GetIPGlobalProperties()
            .GetActiveTcpListeners()
            .Select(e => e.Port)
            .ToHashSet();

        var port = preferredPort;
        while (usedPorts.Contains(port) && port < 65535)
        {
            port++;
        }
        return port;
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
