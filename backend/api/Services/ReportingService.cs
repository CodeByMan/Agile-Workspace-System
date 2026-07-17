using System.Security.Claims;
using api.Data;
using api.Dtos.Reporting;
using api.Interfaces;
using api.Extensions;
using Microsoft.EntityFrameworkCore;
using AgileTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Services;

public sealed class ReportingService : IReportingService
{
    private readonly ApplicationDbContext _context;

    public ReportingService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var monday = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var nextMonday = monday.AddDays(7);

        var projectCounts = await AccessibleProjects(user)
            .GroupBy(_ => 1)
            .Select(g => new { Total = g.Count(), Active = g.Count(x => !x.IsArchived) })
            .FirstOrDefaultAsync(cancellationToken);

        var taskCounts = await AccessibleTasks(user)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                Total = g.Count(),
                ToDo = g.Count(x => x.Status == AgileTaskStatus.ToDo),
                InProgress = g.Count(x => x.Status == AgileTaskStatus.InProgress),
                Done = g.Count(x => x.Status == AgileTaskStatus.Done),
                Overdue = g.Count(x => x.Status != AgileTaskStatus.Done && x.DueDate != null && x.DueDate < today),
                DueThisWeek = g.Count(x =>
                    x.Status != AgileTaskStatus.Done &&
                    x.DueDate != null &&
                    x.DueDate >= monday &&
                    x.DueDate < nextMonday),
                CompletedThisWeek = g.Count(x => x.CompletedAt != null && x.CompletedAt >= monday && x.CompletedAt < nextMonday),
                StoryPoints = g.Sum(x => x.StoryPoints),
                CompletedStoryPoints = g.Where(x => x.Status == AgileTaskStatus.Done).Sum(x => x.StoryPoints)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var tasksByStatus = await AccessibleTasks(user)
            .GroupBy(x => x.Status)
            .Select(g => new TaskStatusCountDto { Status = g.Key.ToString(), Count = g.Count() })
            .OrderBy(x => x.Status)
            .ToListAsync(cancellationToken);

        var topProjects = (await GetProjectProgressInternalAsync(user, cancellationToken))
            .OrderByDescending(x => x.DoneTasks)
            .ThenByDescending(x => x.CompletionRate)
            .Take(5)
            .ToList();

        var totalTasks = taskCounts?.Total ?? 0;
        var doneTasks = taskCounts?.Done ?? 0;
        return new DashboardSummaryDto
        {
            TotalProjects = projectCounts?.Total ?? 0,
            ActiveProjects = projectCounts?.Active ?? 0,
            TotalTasks = totalTasks,
            ToDoTasks = taskCounts?.ToDo ?? 0,
            InProgressTasks = taskCounts?.InProgress ?? 0,
            DoneTasks = doneTasks,
            OverdueTasks = taskCounts?.Overdue ?? 0,
            DueThisWeekTasks = taskCounts?.DueThisWeek ?? 0,
            CompletedThisWeekTasks = taskCounts?.CompletedThisWeek ?? 0,
            TotalStoryPoints = taskCounts?.StoryPoints ?? 0,
            CompletedStoryPoints = taskCounts?.CompletedStoryPoints ?? 0,
            CompletionRate = Percentage(doneTasks, totalTasks),
            TasksByStatus = tasksByStatus,
            TopProjects = topProjects
        };
    }

    public Task<List<ProjectProgressDto>> GetProjectProgressAsync(ClaimsPrincipal user, CancellationToken cancellationToken = default) =>
        GetProjectProgressInternalAsync(user, cancellationToken);

    public async Task<ProjectProgressDto?> GetProjectProgressByIdAsync(
        int projectId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var rows = await GetProjectProgressQuery(user)
            .Where(x => x.ProjectId == projectId)
            .ToListAsync(cancellationToken);
        return rows.Select(MapProjectProgress).FirstOrDefault();
    }

    public async Task<List<UserProductivityDto>> GetUserProductivityAsync(CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var rows = await _context.Users.AsNoTracking()
            .Where(x => x.IsActive)
            .Select(user => new
            {
                UserId = user.Id,
                UserName = user.UserName ?? string.Empty,
                user.FullName,
                AssignedTasks = user.AssignedTasks.Count,
                CompletedTasks = user.AssignedTasks.Count(x => x.Status == AgileTaskStatus.Done),
                InProgressTasks = user.AssignedTasks.Count(x => x.Status == AgileTaskStatus.InProgress),
                OverdueTasks = user.AssignedTasks.Count(x => x.Status != AgileTaskStatus.Done && x.DueDate != null && x.DueDate < today),
                TotalStoryPoints = user.AssignedTasks.Sum(x => x.StoryPoints),
                CompletedStoryPoints = user.AssignedTasks.Where(x => x.Status == AgileTaskStatus.Done).Sum(x => x.StoryPoints)
            })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new UserProductivityDto
        {
            UserId = x.UserId,
            UserName = x.UserName,
            FullName = x.FullName,
            AssignedTasks = x.AssignedTasks,
            CompletedTasks = x.CompletedTasks,
            InProgressTasks = x.InProgressTasks,
            OverdueTasks = x.OverdueTasks,
            TotalStoryPoints = x.TotalStoryPoints,
            CompletedStoryPoints = x.CompletedStoryPoints,
            CompletionRate = Percentage(x.CompletedTasks, x.AssignedTasks)
        })
        .OrderByDescending(x => x.CompletedTasks)
        .ThenByDescending(x => x.CompletionRate)
        .ToList();
    }

    public async Task<BurndownChartDto?> GetBurndownAsync(
        int projectId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var project = await AccessibleProjects(user)
            .Where(x => x.Id == projectId)
            .Select(x => new { x.Id, x.Name, x.StartDate, x.EndDate })
            .FirstOrDefaultAsync(cancellationToken);
        if (project is null) return null;

        // The current schema has no work-item creation timestamp. This chart therefore uses
        // the present project scope and actual completion timestamps without inventing history.
        var tasks = await AccessibleTasks(user)
            .Where(x => x.ProjectId == projectId)
            .Select(x => new { x.StoryPoints, x.CompletedAt })
            .ToListAsync(cancellationToken);

        var startDate = project.StartDate.Date;
        var endDate = (project.EndDate ?? DateTime.UtcNow.Date).Date;
        if (endDate < startDate) endDate = startDate;

        var initialTaskCount = tasks.Count;
        var initialStoryPoints = tasks.Sum(x => x.StoryPoints);
        var points = new List<BurndownPointDto>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
        {
            var completed = tasks.Where(x => x.CompletedAt.HasValue && x.CompletedAt.Value.Date <= date).ToList();
            points.Add(new BurndownPointDto
            {
                Date = date,
                CompletedTasks = completed.Count,
                RemainingTasks = Math.Max(0, initialTaskCount - completed.Count),
                RemainingStoryPoints = Math.Max(0, initialStoryPoints - completed.Sum(x => x.StoryPoints))
            });
        }

        return new BurndownChartDto
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            StartDate = startDate,
            EndDate = endDate,
            InitialTaskCount = initialTaskCount,
            InitialStoryPoints = initialStoryPoints,
            Points = points
        };
    }

    public async Task<List<RecentActivityDto>> GetRecentActivityAsync(
        ClaimsPrincipal user,
        int take = 20,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take <= 0 ? 20 : take, 1, 100);
        var accessibleTaskIds = AccessibleTasks(user).Select(x => x.Id);
        return await _context.TaskActivityLogs.AsNoTracking()
            .Where(x => accessibleTaskIds.Contains(x.TaskItemId))
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new RecentActivityDto
            {
                Id = x.Id,
                TaskId = x.TaskItemId,
                ActivityType = x.ActivityType,
                Description = x.Description,
                OldValue = x.OldValue,
                NewValue = x.NewValue,
                CreatedAt = x.CreatedAt,
                PerformedByUserId = x.PerformedByUserId,
                PerformedByUserName = x.PerformedByUser != null ? x.PerformedByUser.UserName : null,
                TaskTitle = x.TaskItem != null ? x.TaskItem.Title : null,
                ProjectName = x.TaskItem != null && x.TaskItem.Project != null ? x.TaskItem.Project.Name : null
            })
            .ToListAsync(cancellationToken);
    }

    private async Task<List<ProjectProgressDto>> GetProjectProgressInternalAsync(ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var rows = await GetProjectProgressQuery(user)
            .OrderByDescending(x => x.ProjectId)
            .ToListAsync(cancellationToken);
        return rows.Select(MapProjectProgress).ToList();
    }

    private IQueryable<ProjectProgressRow> GetProjectProgressQuery(ClaimsPrincipal user)
    {
        var today = DateTime.UtcNow.Date;
        return AccessibleProjects(user).Select(project => new ProjectProgressRow
        {
            ProjectId = project.Id,
            ProjectName = project.Name,
            IsArchived = project.IsArchived,
            TotalTasks = project.Tasks.Count,
            ToDoTasks = project.Tasks.Count(x => x.Status == AgileTaskStatus.ToDo),
            InProgressTasks = project.Tasks.Count(x => x.Status == AgileTaskStatus.InProgress),
            DoneTasks = project.Tasks.Count(x => x.Status == AgileTaskStatus.Done),
            OverdueTasks = project.Tasks.Count(x => x.Status != AgileTaskStatus.Done && x.DueDate != null && x.DueDate < today),
            TotalStoryPoints = project.Tasks.Sum(x => x.StoryPoints),
            CompletedStoryPoints = project.Tasks.Where(x => x.Status == AgileTaskStatus.Done).Sum(x => x.StoryPoints),
            StartDate = project.StartDate,
            EndDate = project.EndDate
        });
    }

    private static ProjectProgressDto MapProjectProgress(ProjectProgressRow row) => new()
    {
        ProjectId = row.ProjectId,
        ProjectName = row.ProjectName,
        IsArchived = row.IsArchived,
        TotalTasks = row.TotalTasks,
        ToDoTasks = row.ToDoTasks,
        InProgressTasks = row.InProgressTasks,
        DoneTasks = row.DoneTasks,
        OverdueTasks = row.OverdueTasks,
        TotalStoryPoints = row.TotalStoryPoints,
        CompletedStoryPoints = row.CompletedStoryPoints,
        CompletionRate = Percentage(row.DoneTasks, row.TotalTasks),
        StartDate = row.StartDate,
        EndDate = row.EndDate
    };

    private IQueryable<api.Models.Project> AccessibleProjects(ClaimsPrincipal user)
    {
        var query = _context.Projects.AsNoTracking();
        if (ResourceAuthorizationService.IsLeadership(user)) return query;
        var userId = user.GetUserId();
        return query.Where(project =>
            project.CreatedByUserId == userId ||
            project.Tasks.Any(task => task.AssignedToUserId == userId || task.CreatedByUserId == userId) ||
            project.DailyUpdates.Any(update => update.UserId == userId));
    }

    private IQueryable<api.Models.TaskItem> AccessibleTasks(ClaimsPrincipal user)
    {
        var query = _context.TaskItems.AsNoTracking();
        if (ResourceAuthorizationService.IsLeadership(user)) return query;
        var userId = user.GetUserId();
        return query.Where(task => task.AssignedToUserId == userId || task.CreatedByUserId == userId);
    }

    private static decimal Percentage(int completed, int total) =>
        total == 0 ? 0 : Math.Round((decimal)completed / total * 100m, 2);

    private sealed class ProjectProgressRow
    {
        public int ProjectId { get; init; }
        public string ProjectName { get; init; } = string.Empty;
        public bool IsArchived { get; init; }
        public int TotalTasks { get; init; }
        public int ToDoTasks { get; init; }
        public int InProgressTasks { get; init; }
        public int DoneTasks { get; init; }
        public int OverdueTasks { get; init; }
        public int TotalStoryPoints { get; init; }
        public int CompletedStoryPoints { get; init; }
        public DateTime StartDate { get; init; }
        public DateTime? EndDate { get; init; }
    }
}
