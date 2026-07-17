using api.Constants;
using api.Dtos.Common;
using api.Dtos.Users;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/users")]
[ApiController]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager},{AppRoles.TeamLead}")]
public sealed class UsersController(IUserService userService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetUsers(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.GetUsersAsync(
            pageNumber,
            pageSize,
            search,
            role,
            isActive,
            cancellationToken);

        return Ok(ApiResponse<UserListResultDto>.Ok(result));
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(
        string id,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.GetByIdAsync(id, cancellationToken);
        return result is null
            ? NotFound(ApiResponse<object>.Fail("User not found."))
            : Ok(ApiResponse<UserSummaryDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateManagedUserDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.CreateAsync(dto, User, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse<UserSummaryDto>.Ok(result, "User created successfully."));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateUserDto dto,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.UpdateAsync(id, dto, User, cancellationToken);
        return result is null
            ? NotFound(ApiResponse<object>.Fail("User not found."))
            : Ok(ApiResponse<UserSummaryDto>.Ok(result, "User updated successfully."));
    }

    [HttpPatch("{id}/status")]
    public async Task<IActionResult> ToggleStatus(
        string id,
        [FromQuery] bool isActive,
        CancellationToken cancellationToken = default)
    {
        var result = await userService.ToggleStatusAsync(
            id,
            isActive,
            User,
            cancellationToken);

        return result
            ? Ok(ApiResponse<object>.Ok(
                null,
                isActive
                    ? "User activated successfully."
                    : "User deactivated successfully."))
            : NotFound(ApiResponse<object>.Fail("User not found."));
    }
}
