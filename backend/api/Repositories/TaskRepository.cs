using api.Data;
using api.Dtos.Tasks;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<(List<TaskItem> Items, int TotalCount)> GetFilteredAsync(
        TaskQueryParameters query,
        CancellationToken cancellationToken = default)
    {
        var taskQuery = _context.TaskItems
            .AsNoTracking()
            .Include(x => x.Project)
            .Include(x => x.Sprint)
            .Include(x => x.AssignedToUser)
            .Include(x => x.CreatedByUser)
            .Include(x => x.SubTasks)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.AccessibleByUserId))
        {
            var userId = query.AccessibleByUserId;
            taskQuery = taskQuery.Where(x => x.AssignedToUserId == userId || x.CreatedByUserId == userId);
        }

        if (query.ProjectId.HasValue)
            taskQuery = taskQuery.Where(x => x.ProjectId == query.ProjectId.Value);

        if (query.SprintId.HasValue)
            taskQuery = taskQuery.Where(x => x.SprintId == query.SprintId.Value);

        if (query.BacklogOnly == true)
            taskQuery = taskQuery.Where(x => x.SprintId == null);

        if (!string.IsNullOrWhiteSpace(query.AssignedToUserId))
            taskQuery = taskQuery.Where(x => x.AssignedToUserId == query.AssignedToUserId);

        if (query.Status.HasValue)
            taskQuery = taskQuery.Where(x => x.Status == query.Status.Value);

        if (query.Priority.HasValue)
            taskQuery = taskQuery.Where(x => x.Priority == query.Priority.Value);

        if (query.WorkItemType.HasValue)
            taskQuery = taskQuery.Where(x => x.WorkItemType == query.WorkItemType.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            taskQuery = taskQuery.Where(x =>
                EF.Functions.Like(x.Title, $"%{search}%") ||
                EF.Functions.Like(x.Description, $"%{search}%") ||
                EF.Functions.Like(x.AcceptanceCriteria, $"%{search}%"));
        }

        var totalCount = await taskQuery.CountAsync(cancellationToken);

        var items = await taskQuery
            .OrderBy(x => x.SprintId.HasValue ? 0 : 1)
            .ThenByDescending(x => x.Priority)
            .ThenBy(x => x.Status)
            .ThenBy(x => x.DueDate ?? DateTime.MaxValue)
            .ThenBy(x => x.Title)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<TaskItem?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _context.TaskItems
            .AsNoTracking()
            .Include(x => x.Project)
            .Include(x => x.Sprint)
            .Include(x => x.AssignedToUser)
            .Include(x => x.CreatedByUser)
            .Include(x => x.SubTasks)
            .Include(x => x.Comments.OrderByDescending(comment => comment.CreatedAt))
                .ThenInclude(x => x.CreatedByUser)
            .Include(x => x.ActivityLogs.OrderByDescending(log => log.CreatedAt))
                .ThenInclude(x => x.PerformedByUser)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
    }
}
