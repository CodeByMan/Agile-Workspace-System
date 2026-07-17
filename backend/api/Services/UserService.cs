using System.Security.Claims;
using api.Constants;
using api.Data;
using api.Dtos.Users;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Services;

public sealed class UserService(
    ApplicationDbContext context,
    UserManager<AppUser> userManager) : IUserService
{
    public async Task<UserListResultDto> GetUsersAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? role,
        bool? isActive,
        CancellationToken cancellationToken = default)
    {
        pageNumber = Math.Max(1, pageNumber);
        pageSize = Math.Clamp(pageSize <= 0 ? 10 : pageSize, 1, 100);
        var roleFilter = NormalizeOptional(role);

        if (roleFilter is not null && !AppRoles.IsValid(roleFilter))
        {
            throw new BadRequestException("Invalid role filter.");
        }

        var query =
            from user in context.Users.AsNoTracking()
            join userRole in context.UserRoles.AsNoTracking()
                on user.Id equals userRole.UserId
            join identityRole in context.Roles.AsNoTracking()
                on userRole.RoleId equals identityRole.Id
            select new
            {
                User = user,
                Role = identityRole.Name!
            };

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(result =>
                EF.Functions.Like(result.User.UserName!, $"%{term}%") ||
                EF.Functions.Like(result.User.Email!, $"%{term}%") ||
                EF.Functions.Like(result.User.FullName, $"%{term}%"));
        }

        if (isActive.HasValue)
        {
            query = query.Where(result => result.User.IsActive == isActive.Value);
        }

        if (roleFilter is not null)
        {
            query = query.Where(result => result.Role == roleFilter);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(result => result.User.FullName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(result => new UserSummaryDto
            {
                Id = result.User.Id,
                UserName = result.User.UserName ?? string.Empty,
                Email = result.User.Email ?? string.Empty,
                FullName = result.User.FullName,
                JobTitle = result.User.JobTitle,
                PhoneNumber = result.User.PhoneNumber,
                Role = result.Role,
                IsActive = result.User.IsActive,
                AssignedTasks = result.User.AssignedTasks.Count,
                CompletedTasks = result.User.AssignedTasks.Count(
                    task => task.Status == WorkTaskStatus.Done),
                CreatedAt = result.User.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new UserListResultDto
        {
            TotalCount = total,
            PageNumber = pageNumber,
            PageSize = pageSize,
            Items = items
        };
    }

    public async Task<UserSummaryDto?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default)
    {
        return await (
            from user in context.Users.AsNoTracking()
            join userRole in context.UserRoles.AsNoTracking()
                on user.Id equals userRole.UserId
            join role in context.Roles.AsNoTracking()
                on userRole.RoleId equals role.Id
            where user.Id == id
            select new UserSummaryDto
            {
                Id = user.Id,
                UserName = user.UserName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                FullName = user.FullName,
                JobTitle = user.JobTitle,
                PhoneNumber = user.PhoneNumber,
                Role = role.Name ?? string.Empty,
                IsActive = user.IsActive,
                AssignedTasks = user.AssignedTasks.Count,
                CompletedTasks = user.AssignedTasks.Count(
                    task => task.Status == WorkTaskStatus.Done),
                CreatedAt = user.CreatedAt
            }).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<UserSummaryDto> CreateAsync(
        CreateManagedUserDto dto,
        ClaimsPrincipal actingUser,
        CancellationToken cancellationToken = default)
    {
        var actorRole = GetActorRole(actingUser);
        if (!AppRoles.IsValid(dto.Role) ||
            !AppRoles.CanAssignRole(actorRole, dto.Role))
        {
            throw new ForbiddenException("You are not authorized to assign that role.");
        }

        var user = new AppUser
        {
            UserName = dto.UserName.Trim(),
            Email = dto.Email.Trim(),
            FullName = dto.FullName.Trim(),
            JobTitle = NormalizeOptional(dto.JobTitle),
            PhoneNumber = NormalizeOptional(dto.PhoneNumber),
            EmailConfirmed = true,
            IsActive = true
        };

        var normalizedUserName = user.UserName!.ToUpperInvariant();
        var normalizedEmail = user.Email!.ToUpperInvariant();
        var duplicate = await context.Users.AnyAsync(
            candidate =>
                candidate.NormalizedUserName == normalizedUserName ||
                candidate.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (duplicate)
        {
            throw new ConflictException("Username or email already exists.");
        }

        var created = await userManager.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
        {
            throw new BadRequestException(
                string.Join(" ", created.Errors.Select(error => error.Description)));
        }

        var assigned = await userManager.AddToRoleAsync(user, dto.Role);
        if (!assigned.Succeeded)
        {
            await userManager.DeleteAsync(user);
            throw new ConflictException(
                "Role assignment failed; the incomplete account was removed.");
        }

        return (await GetByIdAsync(user.Id, cancellationToken))!;
    }

    public async Task<UserSummaryDto?> UpdateAsync(
        string id,
        UpdateUserDto dto,
        ClaimsPrincipal actingUser,
        CancellationToken cancellationToken = default)
    {
        var actorId = GetActorId(actingUser);
        var actorRole = GetActorRole(actingUser);
        var user = await context.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);

        if (user is null)
        {
            return null;
        }

        var currentRole = (await userManager.GetRolesAsync(user)).FirstOrDefault()
            ?? AppRoles.Developer;
        DemandManage(actorId, actorRole, user.Id, currentRole, dto.Role);

        var email = dto.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var duplicateEmail = await context.Users.AnyAsync(
            candidate =>
                candidate.Id != id &&
                candidate.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (duplicateEmail)
        {
            throw new ConflictException("Another user already uses this email address.");
        }

        await using var transaction = await context.Database.BeginTransactionAsync(
            cancellationToken);

        try
        {
            user.FullName = dto.FullName.Trim();
            user.Email = email;
            user.NormalizedEmail = normalizedEmail;
            user.JobTitle = NormalizeOptional(dto.JobTitle);
            user.PhoneNumber = NormalizeOptional(dto.PhoneNumber);

            var update = await userManager.UpdateAsync(user);
            EnsureSucceeded(update, "User update failed.");

            if (!string.Equals(
                    currentRole,
                    dto.Role,
                    StringComparison.OrdinalIgnoreCase))
            {
                // Add first so a failed assignment never leaves the user without a role.
                var add = await userManager.AddToRoleAsync(user, dto.Role);
                EnsureSucceeded(add, "Role assignment failed.");

                var remove = await userManager.RemoveFromRoleAsync(user, currentRole);
                EnsureSucceeded(remove, "Previous role removal failed.");

                var stamp = await userManager.UpdateSecurityStampAsync(user);
                EnsureSucceeded(stamp, "Security-stamp update failed.");
            }

            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return await GetByIdAsync(id, cancellationToken);
    }

    public async Task<bool> ToggleStatusAsync(
        string id,
        bool isActive,
        ClaimsPrincipal actingUser,
        CancellationToken cancellationToken = default)
    {
        var actorId = GetActorId(actingUser);
        var actorRole = GetActorRole(actingUser);
        var user = await context.Users.FirstOrDefaultAsync(
            candidate => candidate.Id == id,
            cancellationToken);

        if (user is null)
        {
            return false;
        }

        var targetRole = (await userManager.GetRolesAsync(user)).FirstOrDefault()
            ?? AppRoles.Developer;

        if (actorId == id)
        {
            throw new ForbiddenException("You cannot change your own active status.");
        }

        if (!AppRoles.CanManageRole(actorRole, targetRole))
        {
            throw new ForbiddenException("You cannot change this protected account.");
        }

        if (user.IsActive != isActive)
        {
            user.IsActive = isActive;
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            var result = await userManager.UpdateAsync(user);
            EnsureSucceeded(result, "Status update failed.");
        }

        return true;
    }

    private static void DemandManage(
        string actorId,
        string actorRole,
        string targetId,
        string currentRole,
        string requestedRole)
    {
        if (!AppRoles.IsValid(requestedRole))
        {
            throw new BadRequestException("Invalid role supplied.");
        }

        var roleChanged = !string.Equals(
            currentRole,
            requestedRole,
            StringComparison.OrdinalIgnoreCase);

        if (actorId == targetId && roleChanged)
        {
            throw new ForbiddenException(
                "Self-promotion or self-demotion is not permitted.");
        }

        if (actorId != targetId && !AppRoles.CanManageRole(actorRole, currentRole))
        {
            throw new ForbiddenException("You cannot modify this protected account.");
        }

        if (roleChanged && !AppRoles.CanAssignRole(actorRole, requestedRole))
        {
            throw new ForbiddenException("You are not authorized to assign that role.");
        }
    }

    private static string GetActorId(ClaimsPrincipal actor) =>
        actor.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? throw new UnauthorizedException();

    private static string GetActorRole(ClaimsPrincipal actor) =>
        AppRoles.All.FirstOrDefault(actor.IsInRole)
        ?? throw new ForbiddenException();

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static void EnsureSucceeded(
        IdentityResult result,
        string message)
    {
        if (!result.Succeeded)
        {
            throw new ConflictException(
                $"{message} {string.Join(" ", result.Errors.Select(error => error.Description))}");
        }
    }
}
