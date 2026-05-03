using LocationManagement.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocationManagement.Api.Services;

/// <summary>
/// Background service that deletes read notifications older than 30 days daily.
/// </summary>
public sealed class NotificationCleanupService : BackgroundService
{
    private readonly ILogger<NotificationCleanupService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(30);

    /// <summary>
    /// Initializes a new instance of the <see cref="NotificationCleanupService"/> class.
    /// </summary>
    public NotificationCleanupService(ILogger<NotificationCleanupService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Executes the background service.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Notification cleanup service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run at midnight UTC
                var now = DateTime.UtcNow;
                var nextMidnight = now.Date.AddDays(1);
                var delayUntilMidnight = nextMidnight - now;

                _logger.LogInformation("Next notification cleanup run scheduled for {NextMidnight}", nextMidnight);
                await Task.Delay(delayUntilMidnight, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await DeleteOldNotificationsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Notification cleanup service cancelled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in notification cleanup service.");
            }
        }

        _logger.LogInformation("Notification cleanup service stopped.");
    }

    /// <summary>
    /// Deletes read notifications older than the retention period.
    /// </summary>
    private async Task DeleteOldNotificationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTimeOffset.UtcNow.Subtract(_retentionPeriod);
        var deletedCount = await dbContext.Notifications
            .Where(n => n.IsRead && n.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync(stoppingToken);

        _logger.LogInformation("Deleted {DeletedCount} read notifications older than {CutoffDate}", deletedCount, cutoffDate);
    }
}
