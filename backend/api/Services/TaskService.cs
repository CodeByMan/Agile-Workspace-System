using System.Security.Claims;
using api.Data;
using api.Dtos.Tasks;
using api.Exceptions;
using api.Extensions;
using api.Hubs;
using api.Interfaces;
using api.Models;
using api.Models.Enums;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using AgileTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Services;

public sealed class TaskService(
    ITaskRepository taskRepository,
    ApplicationDbContext context,
    IHubContext<TaskCollaborationHub> hub,
    ResourceAuthorizationService authorization,
    ILogger<TaskService> logger) : ITaskService
{
    public async Task<object> GetFilteredAsync(
        TaskQueryParameters query,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        query.PageNumber = Math.Max(query.PageNumber, 1);
        query.PageSize = Math.Clamp(query.PageSize <= 0 ? 20 : query.PageSize, 1, 100);
        ValidateEnums(query.Status, query.Priority, query.WorkItemType);

        if (query.ProjectId.HasValue)
        {
            await authorization.EnsureProjectAccessAsync(user, query.ProjectId.Value, cancellationToken);
        }

        if (!ResourceAuthorizationService.IsLeadership(user))
        {
            query.AccessibleByUserId = user.GetUserId();
        }

        var (items, totalCount) = await taskRepository.GetFilteredAsync(query, cancellationToken);
        return new
        {
            totalCount,
            pageNumber = query.PageNumber,
            pageSize = query.PageSize,
            items = items.Select(MapTaskSummary).ToList()
        };
    }

    public async Task<TaskDto?> GetByIdAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var task = await taskRepository.GetByIdAsync(id, cancellationToken);
        if (task is null || !await authorization.CanAccessTaskAsync(user, task, cancellationToken))
        {
            return null;
        }

        return MapTask(task);
    }

    public async Task<TaskDto> CreateAsync(
        CreateTaskDto dto,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userId = user.GetUserId();
        await authorization.EnsureProjectAccessAsync(user, dto.ProjectId, cancellationToken);
        ValidateInput(
            dto.Title,
            dto.StartDate,
            dto.DueDate,
            dto.IsRecurring,
            dto.RecurrenceRule,
            dto.Status,
            dto.Priority,
            dto.WorkItemType);
        await ValidateReferencesAsync(
            dto.ProjectId,
            dto.SprintId,
            dto.AssignedToUserId,
            dto.ParentTaskId,
            null,
            cancellationToken);

        var entity = new TaskItem
        {
            Title = dto.Title.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            AcceptanceCriteria = dto.AcceptanceCriteria?.Trim() ?? string.Empty,
            WorkItemType = dto.WorkItemType,
            Status = dto.Status,
            Priority = dto.Priority,
            StartDate = dto.StartDate,
            DueDate = dto.DueDate,
            StoryPoints = dto.StoryPoints,
            IsRecurring = dto.IsRecurring,
            RecurrenceRule = dto.IsRecurring ? dto.RecurrenceRule?.Trim() : null,
            ProjectId = dto.ProjectId,
            SprintId = dto.SprintId,
            AssignedToUserId = Clean(dto.AssignedToUserId),
            ParentTaskId = dto.ParentTaskId,
            CreatedByUserId = userId,
            CompletedAt = dto.Status == AgileTaskStatus.Done ? DateTime.UtcNow : null
        };

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await context.TaskItems.AddAsync(entity, cancellationToken);
            await context.TaskActivityLogs.AddAsync(
                Activity(
                    entity,
                    "WorkItemCreated",
                    $"{entity.WorkItemType} '{entity.Title}' created.",
                    null,
                    entity.Status.ToString(),
                    userId),
                cancellationToken);

            if (entity.AssignedToUserId is not null)
            {
                await context.TaskActivityLogs.AddAsync(
                    Activity(
                        entity,
                        "AssignmentChanged",
                        "Work item assigned.",
                        null,
                        entity.AssignedToUserId,
                        userId),
                    cancellationToken);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var created = await taskRepository.GetByIdAsync(entity.Id, cancellationToken)
            ?? throw new ConflictException("Created work item could not be loaded.");
        await SafeBroadcastAsync(created.ProjectId, "workItemCreated", MapTaskSummary(created), cancellationToken);
        return MapTask(created);
    }

    public async Task<TaskDto?> UpdateAsync(
        int id,
        UpdateTaskDto dto,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.TaskItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return null;
        }

        ResourceAuthorizationService.EnsureTaskUpdateAllowed(user, entity, dto);
        ValidateInput(
            dto.Title,
            dto.StartDate,
            dto.DueDate,
            dto.IsRecurring,
            dto.RecurrenceRule,
            dto.Status,
            dto.Priority,
            dto.WorkItemType);
        await ValidateReferencesAsync(
            entity.ProjectId,
            dto.SprintId,
            dto.AssignedToUserId,
            dto.ParentTaskId,
            id,
            cancellationToken);
        ApplyRowVersion(entity, dto.RowVersion);

        var userId = user.GetUserId();
        var oldStatus = entity.Status;
        var oldAssignee = entity.AssignedToUserId;
        var oldSprint = entity.SprintId;

        entity.Title = dto.Title.Trim();
        entity.Description = dto.Description?.Trim() ?? string.Empty;
        entity.AcceptanceCriteria = dto.AcceptanceCriteria?.Trim() ?? string.Empty;
        entity.WorkItemType = dto.WorkItemType;
        entity.Status = dto.Status;
        entity.Priority = dto.Priority;
        entity.StartDate = dto.StartDate;
        entity.DueDate = dto.DueDate;
        entity.StoryPoints = dto.StoryPoints;
        entity.IsRecurring = dto.IsRecurring;
        entity.RecurrenceRule = dto.IsRecurring ? dto.RecurrenceRule?.Trim() : null;
        entity.AssignedToUserId = Clean(dto.AssignedToUserId);
        entity.ParentTaskId = dto.ParentTaskId;
        entity.SprintId = dto.SprintId;
        entity.CompletedAt = dto.Status == AgileTaskStatus.Done
            ? entity.CompletedAt ?? DateTime.UtcNow
            : null;

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            if (oldStatus != entity.Status)
            {
                await context.TaskActivityLogs.AddAsync(
                    Activity(
                        entity,
                        "StatusChanged",
                        $"Status changed from {oldStatus} to {entity.Status}.",
                        oldStatus.ToString(),
                        entity.Status.ToString(),
                        userId),
                    cancellationToken);
            }

            if (oldAssignee != entity.AssignedToUserId)
            {
                await context.TaskActivityLogs.AddAsync(
                    Activity(
                        entity,
                        "AssignmentChanged",
                        "Work item assignment changed.",
                        oldAssignee,
                        entity.AssignedToUserId,
                        userId),
                    cancellationToken);
            }

            if (oldSprint != entity.SprintId)
            {
                await context.TaskActivityLogs.AddAsync(
                    Activity(
                        entity,
                        "SprintChanged",
                        "Sprint assignment changed.",
                        oldSprint?.ToString(),
                        entity.SprintId?.ToString(),
                        userId),
                    cancellationToken);
            }

            await context.TaskActivityLogs.AddAsync(
                Activity(
                    entity,
                    "WorkItemUpdated",
                    $"{entity.WorkItemType} '{entity.Title}' updated.",
                    null,
                    null,
                    userId),
                cancellationToken);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var updated = await taskRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new ConflictException("Updated work item could not be loaded.");
        await SafeBroadcastAsync(updated.ProjectId, "workItemUpdated", MapTaskSummary(updated), cancellationToken);
        return MapTask(updated);
    }

    public async Task<bool> DeleteAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.TaskItems.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null)
        {
            return false;
        }

        ResourceAuthorizationService.EnsureTaskDeletionAllowed(user);
        var projectId = entity.ProjectId;
        context.TaskItems.Remove(entity);
        await context.SaveChangesAsync(cancellationToken);
        await SafeBroadcastAsync(projectId, "workItemDeleted", new { id, projectId }, cancellationToken);
        return true;
    }

    public async Task<TaskCommentDto?> AddCommentAsync(
        int taskId,
        CreateTaskCommentDto dto,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var content = dto.Content.Trim();
        if (content.Length == 0)
        {
            throw new BadRequestException("Comment content is required.");
        }

        if (content.Length > 2000)
        {
            throw new BadRequestException("Comment cannot exceed 2000 characters.");
        }

        var task = await context.TaskItems.FirstOrDefaultAsync(x => x.Id == taskId, cancellationToken);
        if (task is null || !await authorization.CanAccessTaskAsync(user, task, cancellationToken))
        {
            return null;
        }

        var userId = user.GetUserId();
        var comment = new TaskComment
        {
            TaskItemId = taskId,
            Content = content,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = userId
        };

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await context.TaskComments.AddAsync(comment, cancellationToken);
            await context.TaskActivityLogs.AddAsync(
                Activity(
                    task,
                    "CommentAdded",
                    "A comment was added.",
                    null,
                    content,
                    userId),
                cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        var saved = await context.TaskComments
            .AsNoTracking()
            .Include(x => x.CreatedByUser)
            .FirstAsync(x => x.Id == comment.Id, cancellationToken);
        var result = new TaskCommentDto
        {
            Id = saved.Id,
            Content = saved.Content,
            CreatedAt = saved.CreatedAt,
            CreatedByUserId = saved.CreatedByUserId,
            CreatedByUserName = saved.CreatedByUser?.UserName
        };

        await SafeBroadcastAsync(task.ProjectId, "commentAdded", new { taskId, comment = result }, cancellationToken);
        return result;
    }

    private async Task ValidateReferencesAsync(
        int projectId,
        int? sprintId,
        string? assigneeId,
        int? parentId,
        int? currentTaskId,
        CancellationToken cancellationToken)
    {
        if (!await context.Projects.AnyAsync(x => x.Id == projectId, cancellationToken))
        {
            throw new NotFoundException("Project not found.");
        }

        if (sprintId.HasValue)
        {
            var sprint = await context.Sprints
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == sprintId, cancellationToken);
            if (sprint is null)
            {
                throw new NotFoundException("Sprint not found.");
            }

            if (sprint.ProjectId != projectId)
            {
                throw new BadRequestException("Sprint does not belong to the selected project.");
            }

            if (sprint.IsClosed || sprint.EndDate.Date < DateTime.UtcNow.Date)
            {
                throw new ConflictException("Closed sprints cannot accept work items.");
            }
        }

        if (!string.IsNullOrWhiteSpace(assigneeId) &&
            !await context.Users.AnyAsync(x => x.Id == assigneeId && x.IsActive, cancellationToken))
        {
            throw new BadRequestException("Assigned user must exist and be active.");
        }

        if (!parentId.HasValue)
        {
            return;
        }

        if (parentId == currentTaskId)
        {
            throw new BadRequestException("A work item cannot be its own parent.");
        }

        var parent = await context.TaskItems
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == parentId, cancellationToken);
        if (parent is null || parent.ProjectId != projectId)
        {
            throw new BadRequestException("Parent work item must belong to the selected project.");
        }

        var visited = new HashSet<int>();
        var cursor = parent.ParentTaskId;
        while (cursor.HasValue && visited.Add(cursor.Value))
        {
            if (cursor == currentTaskId)
            {
                throw new BadRequestException("The selected parent would create a cycle.");
            }

            cursor = await context.TaskItems
                .Where(x => x.Id == cursor.Value)
                .Select(x => x.ParentTaskId)
                .FirstOrDefaultAsync(cancellationToken);
        }
    }

    private void ApplyRowVersion(TaskItem task, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BadRequestException("Work item row version is required.");
        }

        try
        {
            context.Entry(task).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new BadRequestException("Invalid work item row version.");
        }
    }

    private static void ValidateInput(
        string title,
        DateTime? start,
        DateTime? due,
        bool recurring,
        string? rule,
        AgileTaskStatus status,
        TaskPriority priority,
        WorkItemType type)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new BadRequestException("Title is required.");
        }

        if (title.Trim().Length > 200)
        {
            throw new BadRequestException("Title cannot exceed 200 characters.");
        }

        ValidateEnums(status, priority, type);
        if (start.HasValue && due.HasValue && due < start)
        {
            throw new BadRequestException("Due date cannot precede start date.");
        }

        if (recurring && string.IsNullOrWhiteSpace(rule))
        {
            throw new BadRequestException("A recurrence rule is required for recurring work items.");
        }

        if (rule?.Trim().Length > 250)
        {
            throw new BadRequestException("Recurrence rule cannot exceed 250 characters.");
        }

        if (!recurring && !string.IsNullOrWhiteSpace(rule))
        {
            throw new BadRequestException("A recurrence rule is only valid for recurring work items.");
        }
    }

    private static void ValidateEnums(
        AgileTaskStatus? status,
        TaskPriority? priority,
        WorkItemType? type)
    {
        if (status.HasValue && !Enum.IsDefined(status.Value))
        {
            throw new BadRequestException("Invalid task status.");
        }

        if (priority.HasValue && !Enum.IsDefined(priority.Value))
        {
            throw new BadRequestException("Invalid task priority.");
        }

        if (type.HasValue && !Enum.IsDefined(type.Value))
        {
            throw new BadRequestException("Invalid work item type.");
        }
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static TaskActivityLog Activity(
        TaskItem task,
        string type,
        string description,
        string? oldValue,
        string? newValue,
        string userId) => new()
    {
        TaskItem = task,
        ActivityType = type,
        Description = description,
        OldValue = oldValue,
        NewValue = newValue,
        CreatedAt = DateTime.UtcNow,
        PerformedByUserId = userId
    };

    private async Task SafeBroadcastAsync(
        int projectId,
        string eventName,
        object payload,
        CancellationToken cancellationToken)
    {
        try
        {
            await hub.Clients.Group($"project:{projectId}")
                .SendAsync(eventName, payload, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Realtime notification {EventName} failed after database commit for project {ProjectId}",
                eventName,
                projectId);
        }
    }

    private static object MapTaskSummary(TaskItem task) => new
    {
        task.Id,
        task.Title,
        task.Description,
        task.AcceptanceCriteria,
        task.WorkItemType,
        task.Status,
        task.Priority,
        task.StartDate,
        task.DueDate,
        task.CompletedAt,
        task.StoryPoints,
        task.IsRecurring,
        task.RecurrenceRule,
        task.ProjectId,
        ProjectName = task.Project?.Name,
        task.SprintId,
        SprintName = task.Sprint?.Name,
        task.AssignedToUserId,
        AssignedToUserName = task.AssignedToUser?.UserName,
        task.CreatedByUserId,
        CreatedByUserName = task.CreatedByUser?.UserName,
        task.ParentTaskId,
        SubTaskCount = task.SubTasks.Count,
        CompletedSubTaskCount = task.SubTasks.Count(x => x.IsCompleted),
        RowVersion = Convert.ToBase64String(task.RowVersion)
    };

    private static TaskDto MapTask(TaskItem task) => new()
    {
        Id = task.Id,
        Title = task.Title,
        Description = task.Description,
        AcceptanceCriteria = task.AcceptanceCriteria,
        WorkItemType = task.WorkItemType,
        Status = task.Status,
        Priority = task.Priority,
        StartDate = task.StartDate,
        DueDate = task.DueDate,
        CompletedAt = task.CompletedAt,
        StoryPoints = task.StoryPoints,
        IsRecurring = task.IsRecurring,
        RecurrenceRule = task.RecurrenceRule,
        ProjectId = task.ProjectId,
        ProjectName = task.Project?.Name,
        SprintId = task.SprintId,
        SprintName = task.Sprint?.Name,
        AssignedToUserId = task.AssignedToUserId,
        AssignedToUserName = task.AssignedToUser?.UserName,
        CreatedByUserId = task.CreatedByUserId,
        CreatedByUserName = task.CreatedByUser?.UserName,
        ParentTaskId = task.ParentTaskId,
        SubTaskCount = task.SubTasks.Count,
        CompletedSubTaskCount = task.SubTasks.Count(x => x.IsCompleted),
        RowVersion = Convert.ToBase64String(task.RowVersion),
        Comments = task.Comments.Select(comment => new TaskCommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            CreatedAt = comment.CreatedAt,
            CreatedByUserId = comment.CreatedByUserId,
            CreatedByUserName = comment.CreatedByUser?.UserName
        }).ToList(),
        ActivityLogs = task.ActivityLogs.Select(activity => new TaskActivityLogDto
        {
            Id = activity.Id,
            ActivityType = activity.ActivityType,
            Description = activity.Description,
            OldValue = activity.OldValue,
            NewValue = activity.NewValue,
            CreatedAt = activity.CreatedAt,
            PerformedByUserId = activity.PerformedByUserId,
            PerformedByUserName = activity.PerformedByUser?.UserName
        }).ToList()
    };
}
