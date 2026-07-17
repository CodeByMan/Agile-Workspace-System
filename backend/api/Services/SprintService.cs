using System.Security.Claims;
using api.Data;
using api.Dtos.Sprints;
using api.Exceptions;
using api.Extensions;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using WorkTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Services;

public sealed class SprintService(
    ApplicationDbContext context,
    ResourceAuthorizationService authorization) : ISprintService
{
    public async Task<IReadOnlyList<SprintDto>> GetAllAsync(
        int? projectId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        if (projectId.HasValue)
        {
            await authorization.EnsureProjectAccessAsync(user, projectId.Value, cancellationToken);
        }

        var query = context.Sprints.AsNoTracking().AsQueryable();
        if (projectId.HasValue)
        {
            query = query.Where(x => x.ProjectId == projectId.Value);
        }
        else if (!ResourceAuthorizationService.IsLeadership(user))
        {
            var userId = user.GetUserId();
            query = query.Where(x =>
                x.Project!.CreatedByUserId == userId ||
                x.WorkItems.Any(item =>
                    item.AssignedToUserId == userId ||
                    item.CreatedByUserId == userId));
        }

        var rows = await query
            .OrderByDescending(x => x.StartDate)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Goal,
                x.ProjectId,
                ProjectName = x.Project!.Name,
                x.StartDate,
                x.EndDate,
                x.IsClosed,
                PlannedItemsCount = x.WorkItems.Count,
                CompletedItemsCount = x.WorkItems.Count(item => item.Status == WorkTaskStatus.Done),
                x.RowVersion
            })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new SprintDto
        {
            Id = x.Id,
            Name = x.Name,
            Goal = x.Goal,
            ProjectId = x.ProjectId,
            ProjectName = x.ProjectName,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            IsClosed = x.IsClosed,
            PlannedItemsCount = x.PlannedItemsCount,
            CompletedItemsCount = x.CompletedItemsCount,
            RowVersion = Convert.ToBase64String(x.RowVersion)
        }).ToList();
    }

    public async Task<int> CreateAsync(
        CreateSprintDto dto,
        string userId,
        CancellationToken cancellationToken = default)
    {
        Validate(dto.Name, dto.StartDate, dto.EndDate);

        if (!await context.Projects.AnyAsync(x => x.Id == dto.ProjectId, cancellationToken))
        {
            throw new NotFoundException("Selected project was not found.");
        }

        var sprint = new Sprint
        {
            Name = dto.Name.Trim(),
            Goal = dto.Goal?.Trim() ?? string.Empty,
            ProjectId = dto.ProjectId,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            CreatedByUserId = userId
        };

        await context.Sprints.AddAsync(sprint, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        return sprint.Id;
    }

    public async Task<SprintMutationResult?> UpdateAsync(
        int id,
        UpdateSprintDto dto,
        CancellationToken cancellationToken = default)
    {
        Validate(dto.Name, dto.StartDate, dto.EndDate);

        var sprint = await context.Sprints.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (sprint is null)
        {
            return null;
        }

        ApplyRowVersion(sprint, dto.RowVersion);
        if (sprint.IsClosed && !dto.IsClosed)
        {
            throw new ConflictException("A closed sprint cannot be reopened.");
        }

        sprint.Name = dto.Name.Trim();
        sprint.Goal = dto.Goal?.Trim() ?? string.Empty;
        sprint.StartDate = dto.StartDate;
        sprint.EndDate = dto.EndDate;
        sprint.IsClosed = dto.IsClosed;

        await context.SaveChangesAsync(cancellationToken);
        return new SprintMutationResult(sprint.Id, Convert.ToBase64String(sprint.RowVersion));
    }

    private void ApplyRowVersion(Sprint sprint, string rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            throw new BadRequestException("Sprint row version is required.");
        }

        try
        {
            context.Entry(sprint).Property(x => x.RowVersion).OriginalValue =
                Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            throw new BadRequestException("Invalid sprint row version.");
        }
    }

    private static void Validate(string name, DateTime startDate, DateTime endDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Sprint name is required.");
        }

        if (name.Trim().Length > 160)
        {
            throw new BadRequestException("Sprint name cannot exceed 160 characters.");
        }

        if (endDate.Date < startDate.Date)
        {
            throw new BadRequestException("Sprint end date must be on or after the start date.");
        }
    }
}
