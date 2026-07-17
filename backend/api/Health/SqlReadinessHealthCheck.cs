using api.Data;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace api.Health;

public sealed class SqlReadinessHealthCheck(ApplicationDbContext context) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext healthContext, CancellationToken cancellationToken = default)
    {
        try
        {
            return await context.Database.CanConnectAsync(cancellationToken)
                ? HealthCheckResult.Healthy("SQL Server is reachable.")
                : HealthCheckResult.Unhealthy("SQL Server is not reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("SQL Server readiness check failed.", ex);
        }
    }
}
