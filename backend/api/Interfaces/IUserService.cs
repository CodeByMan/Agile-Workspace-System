using System.Security.Claims;
using api.Dtos.Users;

namespace api.Interfaces;

public interface IUserService
{
    Task<UserListResultDto> GetUsersAsync(
        int pageNumber,
        int pageSize,
        string? search,
        string? role,
        bool? isActive,
        CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken = default);

    Task<UserSummaryDto> CreateAsync(
        CreateManagedUserDto dto,
        ClaimsPrincipal actingUser,
        CancellationToken cancellationToken = default);

    Task<UserSummaryDto?> UpdateAsync(
        string id,
        UpdateUserDto dto,
        ClaimsPrincipal actingUser,
        CancellationToken cancellationToken = default);

    Task<bool> ToggleStatusAsync(
        string id,
        bool isActive,
        ClaimsPrincipal actingUser,
        CancellationToken cancellationToken = default);
}
