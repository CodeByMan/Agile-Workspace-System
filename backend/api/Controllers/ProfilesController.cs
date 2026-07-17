using api.Dtos.Common;
using api.Dtos.Profile;
using api.Extensions;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/profile")]
[ApiController]
[Authorize]
public class ProfilesController : ControllerBase
{
    private readonly IProfileService _profileService;

    public ProfilesController(IProfileService profileService)
    {
        _profileService = profileService;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken = default)
    {
        var result = await _profileService.GetAsync(User.GetUserId(), cancellationToken);
        return result is null
            ? NotFound(ApiResponse<object>.Fail("Profile not found."))
            : Ok(ApiResponse<ProfileDto>.Ok(result));
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateProfileDto dto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _profileService.UpdateAsync(User.GetUserId(), dto, cancellationToken);
        return result is null
            ? NotFound(ApiResponse<object>.Fail("Profile not found."))
            : Ok(ApiResponse<ProfileDto>.Ok(result, "Profile updated successfully."));
    }

    [HttpPut("me/password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto, CancellationToken cancellationToken = default)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _profileService.ChangePasswordAsync(User.GetUserId(), dto, cancellationToken);
        if (!result.Success)
        {
            return BadRequest(ApiResponse<object>.Fail(result.Message, result.Errors));
        }

        return Ok(ApiResponse<object>.Ok(null, result.Message));
    }
}
