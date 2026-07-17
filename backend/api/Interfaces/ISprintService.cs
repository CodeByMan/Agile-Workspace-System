using System.Security.Claims;
using api.Dtos.Sprints;

namespace api.Interfaces;

public sealed record SprintMutationResult(int Id, string RowVersion);

public interface ISprintService
{
    Task<IReadOnlyList<SprintDto>> GetAllAsync(
        int? projectId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<int> CreateAsync(
        CreateSprintDto dto,
        string userId,
        CancellationToken cancellationToken = default);

    Task<SprintMutationResult?> UpdateAsync(
        int id,
        UpdateSprintDto dto,
        CancellationToken cancellationToken = default);
}
