using api.Dtos.Profile;

namespace api.Interfaces;

public interface IProfileService
{
    Task<ProfileDto?> GetAsync(
        string userId,
        CancellationToken cancellationToken = default);

    Task<ProfileDto?> UpdateAsync(
        string userId,
        UpdateProfileDto dto,
        CancellationToken cancellationToken = default);

    Task<(
        bool Success,
        string Message,
        Dictionary<string, string[]>? Errors)> ChangePasswordAsync(
            string userId,
            ChangePasswordDto dto,
            CancellationToken cancellationToken = default);
}
