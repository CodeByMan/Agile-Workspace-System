using api.Data;
using api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace api.Services;

public sealed class AuditLogRetentionService(
    IServiceScopeFactory scopeFactory,
    IOptionsMonitor<AuditLoggingOptions> optionsMonitor,
    ILogger<AuditLogRetentionService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await DelaySafelyAsync(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = optionsMonitor.CurrentValue;
            if (options.RetentionCleanupEnabled)
            {
                await PurgeExpiredLogsAsync(options, stoppingToken);
            }

            var intervalHours = Math.Clamp(options.CleanupIntervalHours, 1, 24 * 7);
            await DelaySafelyAsync(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }

    private async Task PurgeExpiredLogsAsync(
        AuditLoggingOptions options,
        CancellationToken cancellationToken)
    {
        var retentionDays = Math.Clamp(options.RetentionDays, 1, 3650);
        var cutoffUtc = DateTime.UtcNow.AddDays(-retentionDays);

        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var apiLogsDeleted = await context.ApiLogs
                .Where(log => log.Timestamp < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken);
            var errorLogsDeleted = await context.ErrorLogs
                .Where(log => log.Timestamp < cutoffUtc)
                .ExecuteDeleteAsync(cancellationToken);

            if (apiLogsDeleted > 0 || errorLogsDeleted > 0)
            {
                logger.LogInformation(
                    "Audit retention removed {ApiLogCount} API logs and {ErrorLogCount} error logs older than {RetentionDays} days.",
                    apiLogsDeleted,
                    errorLogsDeleted,
                    retentionDays);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Audit retention cleanup failed. Business requests are not affected.");
        }
    }

    private static async Task DelaySafelyAsync(
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(delay, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Normal shutdown.
        }
    }
}
