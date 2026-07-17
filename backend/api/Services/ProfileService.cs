using api.Data;
using api.Dtos.Profile;
using api.Exceptions;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class ProfileService(
    ApplicationDbContext context,
    UserManager<AppUser> userManager) : IProfileService
{
    public async Task<ProfileDto?> GetAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var role = (await userManager.GetRolesAsync(user)).FirstOrDefault() ?? string.Empty;
        return new ProfileDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            JobTitle = user.JobTitle,
            PhoneNumber = user.PhoneNumber,
            Role = role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }

    public async Task<ProfileDto?> UpdateAsync(
        string userId,
        UpdateProfileDto dto,
        CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        var email = dto.Email.Trim();
        var normalizedEmail = email.ToUpperInvariant();
        var duplicateEmail = await context.Users.AnyAsync(
            x => x.Id != userId && x.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (duplicateEmail)
        {
            throw new ConflictException("Another user already uses this email address.");
        }

        user.FullName = dto.FullName.Trim();
        user.Email = email;
        user.NormalizedEmail = normalizedEmail;
        user.JobTitle = NormalizeOptional(dto.JobTitle);
        user.PhoneNumber = NormalizeOptional(dto.PhoneNumber);

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new ConflictException(
                result.Errors.FirstOrDefault()?.Description ?? "Profile update failed.");
        }

        return await GetAsync(userId, cancellationToken);
    }

    public async Task<(
        bool Success,
        string Message,
        Dictionary<string, string[]>? Errors)> ChangePasswordAsync(
            string userId,
            ChangePasswordDto dto,
            CancellationToken cancellationToken = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken);

        if (user is null)
        {
            return (false, "User not found.", null);
        }

        var previousStamp = user.SecurityStamp;
        var result = await userManager.ChangePasswordAsync(
            user,
            dto.CurrentPassword,
            dto.NewPassword);

        if (!result.Succeeded)
        {
            var errors = result.Errors
                .GroupBy(x => x.Code)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(error => error.Description).ToArray());

            return (false, "Unable to change password.", errors);
        }

        if (string.Equals(previousStamp, user.SecurityStamp, StringComparison.Ordinal))
        {
            var stampResult = await userManager.UpdateSecurityStampAsync(user);
            if (!stampResult.Succeeded)
            {
                throw new ConflictException(
                    "The password changed, but the security session could not be invalidated. Sign out and contact an administrator.");
            }
        }

        return (true, "Password updated successfully.", null);
    }

    private static string? NormalizeOptional(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
