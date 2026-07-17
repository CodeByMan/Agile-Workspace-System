using System.Security.Claims;
using api.Constants;
using api.Data;
using api.Dtos.Tasks;
using api.Exceptions;
using api.Extensions;
using api.Models;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class ResourceAuthorizationService(ApplicationDbContext context)
{
    public static bool IsLeadership(ClaimsPrincipal user) => AppRoles.DeliveryLeadership.Any(user.IsInRole);

    public async Task<bool> CanAccessProjectAsync(
        ClaimsPrincipal user,
        int projectId,
        CancellationToken cancellationToken = default)
    {
        if (IsLeadership(user))
        {
            return await context.Projects.AnyAsync(x => x.Id == projectId, cancellationToken);
        }

        var userId = user.GetUserId();
        return await context.Projects.AnyAsync(x =>
            x.Id == projectId &&
            (x.CreatedByUserId == userId ||
             x.Tasks.Any(t => t.AssignedToUserId == userId || t.CreatedByUserId == userId) ||
             x.DailyUpdates.Any(d => d.UserId == userId)), cancellationToken);
    }

    public Task<bool> CanAccessTaskAsync(
        ClaimsPrincipal user,
        TaskItem task,
        CancellationToken cancellationToken = default)
    {
        var userId = user.GetUserId();
        return Task.FromResult(
            IsLeadership(user) ||
            task.AssignedToUserId == userId ||
            task.CreatedByUserId == userId);
    }

    public async Task<bool> CanJoinProjectRealtimeAsync(
        ClaimsPrincipal user,
        int projectId,
        CancellationToken cancellationToken = default) =>
        IsLeadership(user) && await context.Projects.AnyAsync(x => x.Id == projectId, cancellationToken);

    public static bool CanModifyTask(ClaimsPrincipal user, TaskItem task)
    {
        var userId = user.GetUserId();
        return IsLeadership(user) || task.AssignedToUserId == userId || task.CreatedByUserId == userId;
    }

    public async Task EnsureProjectAccessAsync(
        ClaimsPrincipal user,
        int projectId,
        CancellationToken cancellationToken = default)
    {
        if (!await context.Projects.AnyAsync(x => x.Id == projectId, cancellationToken))
        {
            throw new NotFoundException("Project not found.");
        }

        if (!await CanAccessProjectAsync(user, projectId, cancellationToken))
        {
            throw new ForbiddenException("You do not have access to this project.");
        }
    }

    public static void EnsureTaskModification(ClaimsPrincipal user, TaskItem task)
    {
        if (!CanModifyTask(user, task))
        {
            throw new ForbiddenException("You may only update work items assigned to or created by you.");
        }
    }

    public static void EnsureTaskDeletionAllowed(ClaimsPrincipal user)
    {
        if (!user.IsInRole(AppRoles.Admin) &&
            !user.IsInRole(AppRoles.ScrumMaster) &&
            !user.IsInRole(AppRoles.Manager))
        {
            throw new ForbiddenException("Deleting work items requires an Admin, Scrum Master, or Manager role.");
        }
    }

    public static void EnsureTaskUpdateAllowed(
        ClaimsPrincipal user,
        TaskItem current,
        UpdateTaskDto requested)
    {
        EnsureTaskModification(user, current);
        if (IsLeadership(user))
        {
            return;
        }

        var changesPlanningFields =
            current.SprintId != requested.SprintId ||
            !SameOptionalText(current.AssignedToUserId, requested.AssignedToUserId) ||
            current.ParentTaskId != requested.ParentTaskId ||
            current.IsRecurring != requested.IsRecurring ||
            !SameOptionalText(current.RecurrenceRule, requested.RecurrenceRule) ||
            current.WorkItemType != requested.WorkItemType ||
            current.Priority != requested.Priority ||
            current.StoryPoints != requested.StoryPoints ||
            current.StartDate != requested.StartDate ||
            current.DueDate != requested.DueDate;

        if (changesPlanningFields)
        {
            throw new ForbiddenException(
                "Developers may update content and status on their own work items. " +
                "Planning, assignment, sprint, parent, recurrence, priority, " +
                "estimate, and schedule changes require a delivery-leadership role.");
        }
    }

    private static bool SameOptionalText(string? left, string? right) =>
        string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);

    private static string? Normalize(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
