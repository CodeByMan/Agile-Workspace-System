using System.Security.Claims;
using api.Constants;
using api.Data;
using api.Dtos.DailyUpdates;
using api.Exceptions;
using api.Extensions;
using api.Interfaces;
using api.Models;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class DailyUpdateService(
    ApplicationDbContext context,
    ResourceAuthorizationService authorization) : IDailyUpdateService
{
    public async Task<DailyUpdatePageDto> GetAllAsync(
        int? projectId,
        DateTime? date,
        int pageNumber,
        int pageSize,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize <= 0 ? 25 : pageSize, 1, 100);
        var targetDate = (date ?? DateTime.UtcNow.Date).Date;

        if (projectId.HasValue)
        {
            await authorization.EnsureProjectAccessAsync(user, projectId.Value, cancellationToken);
        }

        var query = context.DailyUpdates
            .AsNoTracking()
            .Where(x => x.UpdateDate == targetDate);

        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId.Value);
        }
        else if (!ResourceAuthorizationService.IsLeadership(user))
        {
            var userId = user.GetUserId();
            query = query.Where(x =>
                x.UserId == userId ||
                x.Project!.Tasks.Any(task =>
                    task.AssignedToUserId == userId ||
                    task.CreatedByUserId == userId));
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new DailyUpdateDto
            {
                Id = x.Id,
                ProjectId = x.ProjectId,
                ProjectName = x.Project!.Name,
                SprintId = x.SprintId,
                SprintName = x.Sprint != null ? x.Sprint.Name : null,
                UserId = x.UserId,
                UserName = x.User!.UserName ?? string.Empty,
                FullName = x.User.FullName,
                Yesterday = x.Yesterday,
                TodayPlan = x.TodayPlan,
                Blockers = x.Blockers,
                UpdateDate = x.UpdateDate,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new DailyUpdatePageDto
        {
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<DailyUpdateCreateResult> CreateAsync(
        CreateDailyUpdateDto dto,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var userId = user.GetUserId();
        var updateDate = (dto.UpdateDate ?? DateTime.UtcNow.Date).Date;

        await authorization.EnsureProjectAccessAsync(user, dto.ProjectId, cancellationToken);
        ValidateText(dto);

        if (dto.SprintId.HasValue &&
            !await context.Sprints.AnyAsync(
                x => x.Id == dto.SprintId.Value && x.ProjectId == dto.ProjectId,
                cancellationToken))
        {
            throw new BadRequestException("Selected sprint does not belong to the selected project.");
        }

        var entity = new DailyUpdate
        {
            ProjectId = dto.ProjectId,
            SprintId = dto.SprintId,
            UserId = userId,
            Yesterday = dto.Yesterday.Trim(),
            TodayPlan = dto.TodayPlan.Trim(),
            Blockers = dto.Blockers?.Trim() ?? string.Empty,
            UpdateDate = updateDate,
            CreatedAt = DateTime.UtcNow
        };

        var project = await context.Projects
            .AsNoTracking()
            .Where(x => x.Id == dto.ProjectId)
            .Select(x => new
            {
                x.Name,
                x.CreatedByUserId,
                CreatorIsActive = x.CreatedByUser!.IsActive
            })
            .SingleAsync(cancellationToken);

        var leadershipRoleIds = context.Roles
            .Where(role => AppRoles.DeliveryLeadership.Contains(role.Name!))
            .Select(role => role.Id);

        var recipientIds = await (
            from userRole in context.UserRoles
            join recipient in context.Users on userRole.UserId equals recipient.Id
            where leadershipRoleIds.Contains(userRole.RoleId) &&
                  recipient.Id != userId &&
                  recipient.IsActive
            select recipient.Id)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (project.CreatorIsActive &&
            project.CreatedByUserId != userId &&
            !recipientIds.Contains(project.CreatedByUserId))
        {
            recipientIds.Add(project.CreatedByUserId);
        }

        var submittedBy = user.Identity?.Name ?? "A team member";
        var createdAt = DateTime.UtcNow;
        var notifications = recipientIds.Select(recipientId => new Notification
        {
            UserId = recipientId,
            Type = "daily-update",
            Title = "New daily update submitted",
            Message = $"{submittedBy} submitted a daily update for {project.Name}.",
            Link = "/sprints",
            IsRead = false,
            CreatedAt = createdAt
        }).ToList();

        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            await context.DailyUpdates.AddAsync(entity, cancellationToken);
            await context.Notifications.AddRangeAsync(notifications, cancellationToken);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch (DbUpdateException exception) when (IsUniqueConstraintViolation(exception))
        {
            await transaction.RollbackAsync(cancellationToken);
            throw new ConflictException("A daily update already exists for this project and date.");
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return new DailyUpdateCreateResult(entity.Id, dto.ProjectId, updateDate);
    }

    private static void ValidateText(CreateDailyUpdateDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Yesterday) ||
            string.IsNullOrWhiteSpace(dto.TodayPlan))
        {
            throw new BadRequestException("Yesterday and today plan are required.");
        }
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception)
    {
        if (exception.InnerException is SqlException { Number: 2601 or 2627 })
        {
            return true;
        }

        var innerType = exception.InnerException?.GetType().Name;
        var innerMessage = exception.InnerException?.Message ?? string.Empty;
        return string.Equals(innerType, "SqliteException", StringComparison.Ordinal) &&
               innerMessage.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase);
    }
}
