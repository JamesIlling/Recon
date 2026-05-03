using LocationManagement.Api.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocationManagement.Api.Services;

/// <summary>
/// Background service that purges audit events older than 1 year daily.
/// </summary>
public sealed class AuditRetentionService : BackgroundService
{
    private readonly ILogger<AuditRetentionService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly TimeSpan _retentionPeriod = TimeSpan.FromDays(365);

    /// <summary>
    /// Initializes a new instance of the <see cref="AuditRetentionService"/> class.
    /// </summary>
    public AuditRetentionService(ILogger<AuditRetentionService> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    /// <summary>
    /// Executes the background service.
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Audit retention service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Run at midnight UTC
                var now = DateTime.UtcNow;
                var nextMidnight = now.Date.AddDays(1);
                var delayUntilMidnight = nextMidnight - now;

                _logger.LogInformation("Next audit retention run scheduled for {NextMidnight}", nextMidnight);
                await Task.Delay(delayUntilMidnight, stoppingToken);

                if (stoppingToken.IsCancellationRequested)
                {
                    break;
                }

                await PurgeOldAuditEventsAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Audit retention service cancelled.");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in audit retention service.");
            }
        }

        _logger.LogInformation("Audit retention service stopped.");
    }

    /// <summary>
    /// Purges audit events older than the retention period.
    /// </summary>
    private async Task PurgeOldAuditEventsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTimeOffset.UtcNow.Subtract(_retentionPeriod);
        var deletedCount = await dbContext.AuditEvents
            .Where(ae => ae.CreatedAt < cutoffDate)
            .ExecuteDeleteAsync(stoppingToken);

        _logger.LogInformation("Purged {DeletedCount} audit events older than {CutoffDate}", deletedCount, cutoffDate);
    }
}
