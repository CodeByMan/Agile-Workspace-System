using api.Models.Log;

namespace api.Interfaces;

public interface IErrorLogRepository
{
    Task<ErrorLog> CreateAsync(ErrorLog errorLog, CancellationToken cancellationToken = default);
}
