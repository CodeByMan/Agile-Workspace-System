using api.Data;
using api.Interfaces;
using api.Models.Log;

namespace api.Repositories;

public class ApiLogRepository(ApplicationDbContext context) : IApiLogRepository
{
    public async Task<ApiLog> CreateAsync(ApiLog apiLog, CancellationToken cancellationToken = default)
    {
        context.ApiLogs.Add(apiLog);
        await context.SaveChangesAsync(cancellationToken);
        return apiLog;
    }
}
