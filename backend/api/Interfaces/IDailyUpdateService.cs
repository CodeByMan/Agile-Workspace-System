using System.Security.Claims;
using api.Dtos.DailyUpdates;

namespace api.Interfaces;

public sealed record DailyUpdateCreateResult(int Id, int ProjectId, DateTime UpdateDate);

public interface IDailyUpdateService
{
    Task<DailyUpdatePageDto> GetAllAsync(
        int? projectId,
        DateTime? date,
        int pageNumber,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<DailyUpdateCreateResult> CreateAsync(
        CreateDailyUpdateDto dto,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
