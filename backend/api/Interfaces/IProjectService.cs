using System.Security.Claims;
using api.Dtos.Projects;

namespace api.Interfaces;

public interface IProjectService
{
    Task<List<ProjectDto>> GetAllAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ProjectDto?> GetByIdAsync(int id, ClaimsPrincipal user, CancellationToken cancellationToken = default);
    Task<ProjectDto> CreateAsync(CreateProjectDto dto, string userId, CancellationToken cancellationToken = default);
    Task<ProjectDto?> UpdateAsync(int id, UpdateProjectDto dto, CancellationToken cancellationToken = default);
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
