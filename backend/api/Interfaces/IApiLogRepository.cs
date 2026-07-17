using api.Models.Log;

namespace api.Interfaces;

public interface IApiLogRepository
{
    Task<ApiLog> CreateAsync(ApiLog apiLog, CancellationToken cancellationToken = default);
}
