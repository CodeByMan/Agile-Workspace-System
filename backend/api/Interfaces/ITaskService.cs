using System.Security.Claims;
using api.Dtos.Tasks;

namespace api.Interfaces;

public interface ITaskService
{
    Task<object> GetFilteredAsync(
        TaskQueryParameters query,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<TaskDto?> GetByIdAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<TaskDto> CreateAsync(
        CreateTaskDto dto,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<TaskDto?> UpdateAsync(
        int id,
        UpdateTaskDto dto,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<TaskCommentDto?> AddCommentAsync(
        int taskId,
        CreateTaskCommentDto dto,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);
}
