using api.Data;
using api.Interfaces;
using api.Models.Log;

namespace api.Repositories;

public class ErrorLogRepository(ApplicationDbContext context) : IErrorLogRepository
{
    public async Task<ErrorLog> CreateAsync(ErrorLog errorLog, CancellationToken cancellationToken = default)
    {
        context.ErrorLogs.Add(errorLog);
        await context.SaveChangesAsync(cancellationToken);
        return errorLog;
    }
}
