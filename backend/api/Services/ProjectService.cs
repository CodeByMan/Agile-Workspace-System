using System.Security.Claims;
using api.Data;
using api.Dtos.Projects;
using api.Exceptions;
using api.Extensions;
using api.Interfaces;
using api.Models;
using Microsoft.EntityFrameworkCore;
using AgileTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Services;

public sealed class ProjectService(ApplicationDbContext context) : IProjectService
{
    public async Task<List<ProjectDto>> GetAllAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var rows = await AccessibleProjects(user)
            .OrderByDescending(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.StartDate,
                x.EndDate,
                x.IsArchived,
                x.CreatedByUserId,
                CreatedByUserName = x.CreatedByUser!.UserName,
                TotalTasks = x.Tasks.Count,
                CompletedTasks = x.Tasks.Count(t => t.Status == AgileTaskStatus.Done),
                x.RowVersion
            })
            .ToListAsync(cancellationToken);

        return rows.Select(x => new ProjectDto
        {
            Id = x.Id,
            Name = x.Name,
            Description = x.Description,
            StartDate = x.StartDate,
            EndDate = x.EndDate,
            IsArchived = x.IsArchived,
            CreatedByUserId = x.CreatedByUserId,
            CreatedByUserName = x.CreatedByUserName,
            TotalTasks = x.TotalTasks,
            CompletedTasks = x.CompletedTasks,
            RowVersion = Convert.ToBase64String(x.RowVersion)
        }).ToList();
    }

    public async Task<ProjectDto?> GetByIdAsync(
        int id,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default)
    {
        var row = await AccessibleProjects(user)
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.Name,
                x.Description,
                x.StartDate,
                x.EndDate,
                x.IsArchived,
                x.CreatedByUserId,
                CreatedByUserName = x.CreatedByUser!.UserName,
                TotalTasks = x.Tasks.Count,
                CompletedTasks = x.Tasks.Count(t => t.Status == AgileTaskStatus.Done),
                x.RowVersion
            })
            .FirstOrDefaultAsync(cancellationToken);

        return row is null
            ? null
            : new ProjectDto
            {
                Id = row.Id,
                Name = row.Name,
                Description = row.Description,
                StartDate = row.StartDate,
                EndDate = row.EndDate,
                IsArchived = row.IsArchived,
                CreatedByUserId = row.CreatedByUserId,
                CreatedByUserName = row.CreatedByUserName,
                TotalTasks = row.TotalTasks,
                CompletedTasks = row.CompletedTasks,
                RowVersion = Convert.ToBase64String(row.RowVersion)
            };
    }

    public async Task<ProjectDto> CreateAsync(
        CreateProjectDto dto,
        string userId,
        CancellationToken cancellationToken = default)
    {
        Validate(dto.Name, dto.StartDate, dto.EndDate);

        var project = new Project
        {
            Name = dto.Name.Trim(),
            Description = dto.Description?.Trim() ?? string.Empty,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            CreatedByUserId = userId
        };

        await context.Projects.AddAsync(project, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);

        var created = await context.Projects
            .AsNoTracking()
            .Include(x => x.CreatedByUser)
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(x => x.Id == project.Id, cancellationToken)
            ?? throw new ConflictException("Created project could not be loaded.");

        return Map(created);
    }

    public async Task<ProjectDto?> UpdateAsync(
        int id,
        UpdateProjectDto dto,
        CancellationToken cancellationToken = default)
    {
        Validate(dto.Name, dto.StartDate, dto.EndDate);

        var project = await context.Projects
            .Include(x => x.CreatedByUser)
            .Include(x => x.Tasks)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (project is null)
        {
            return null;
        }

        ApplyRowVersion(project, dto.RowVersion);
        project.Name = dto.Name.Trim();
        project.Description = dto.Description?.Trim() ?? string.Empty;
        project.StartDate = dto.StartDate;
        project.EndDate = dto.EndDate;
        project.IsArchived = dto.IsArchived;

        await context.SaveChangesAsync(cancellationToken);
        return Map(project);
    }

    public async Task<bool> DeleteAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var project = await context.Projects.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (project is null)
        {
            return false;
        }

        if (await context.TaskItems.AnyAsync(x => x.ProjectId == id, cancellationToken))
        {
            throw new ConflictException("A project with work items cannot be deleted. Archive it instead.");
        }

        context.Projects.Remove(project);
        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private IQueryable<Project> AccessibleProjects(ClaimsPrincipal user)
    {
        var query = context.Projects.AsNoTracking();
        if (ResourceAuthorizationService.IsLeadership(user))
        {
            return query;
        }

        var userId = user.GetUserId();
        return query.Where(x =>
            x.CreatedByUserId == userId ||
            x.Tasks.Any(t => t.AssignedToUserId == userId || t.CreatedByUserId == userId) ||
            x.DailyUpdates.Any(d => d.UserId == userId));
    }

    private void ApplyRowVersion(Project project, string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new BadRequestException("Project row version is required.");
        }

        try
        {
            context.Entry(project).Property(x => x.RowVersion).OriginalValue = Convert.FromBase64String(value);
        }
        catch (FormatException)
        {
            throw new BadRequestException("Invalid project row version.");
        }
    }

    private static void Validate(string name, DateTime start, DateTime? end)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new BadRequestException("Project name is required.");
        }

        if (name.Trim().Length > 160)
        {
            throw new BadRequestException("Project name cannot exceed 160 characters.");
        }

        if (end.HasValue && end.Value.Date < start.Date)
        {
            throw new BadRequestException("Project end date cannot precede its start date.");
        }
    }

    private static ProjectDto Map(Project project) => new()
    {
        Id = project.Id,
        Name = project.Name,
        Description = project.Description,
        StartDate = project.StartDate,
        EndDate = project.EndDate,
        IsArchived = project.IsArchived,
        CreatedByUserId = project.CreatedByUserId,
        CreatedByUserName = project.CreatedByUser?.UserName,
        TotalTasks = project.Tasks.Count,
        CompletedTasks = project.Tasks.Count(t => t.Status == AgileTaskStatus.Done),
        RowVersion = Convert.ToBase64String(project.RowVersion)
    };
}
