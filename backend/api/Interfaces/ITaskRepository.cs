using api.Dtos.Tasks;
using api.Models;

namespace api.Interfaces;

public interface ITaskRepository
{
    Task<(List<TaskItem> Items, int TotalCount)> GetFilteredAsync(
        TaskQueryParameters query,
        CancellationToken cancellationToken = default);

    Task<TaskItem?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default);
}
