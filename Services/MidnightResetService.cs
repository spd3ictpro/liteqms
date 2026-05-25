using LiteQMS.Data;
using LiteQMS.Services;
using Microsoft.EntityFrameworkCore;

namespace LiteQMS.Services;

public class MidnightResetService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MidnightResetService> _logger;
    private readonly QueueStateService _queueState;

    public MidnightResetService(
        IServiceProvider serviceProvider,
        ILogger<MidnightResetService> logger,
        QueueStateService queueState)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _queueState = queueState;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await PerformResetAsync(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var now = DateTime.Now;
            var nextMidnight = now.Date.AddDays(1);
            var delay = nextMidnight - now;

            _logger.LogInformation("Midnight reset scheduled in {Hours}h {Minutes}m",
                delay.TotalHours, delay.Minutes);

            try
            {
                await Task.Delay(delay, stoppingToken);
            }
            catch (TaskCanceledException)
            {
                break;
            }

            await PerformResetAsync(stoppingToken);
        }
    }

    private async Task PerformResetAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Performing queue reset and cleanup");

        try
        {
            await _queueState.ResetStateAsync();

            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var cutoff = DateTime.Now.Date.AddDays(-120);
            var deleted = await db.CallRecords
                .Where(r => r.Timestamp < cutoff)
                .ExecuteDeleteAsync(stoppingToken);

            _logger.LogInformation("Reset completed — deleted {Count} records older than 120 days", deleted);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Reset failed");
        }
    }
}
