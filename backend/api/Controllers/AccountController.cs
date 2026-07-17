using api.Constants;
using api.Dtos.Account;
using api.Dtos.Common;
using api.Extensions;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace api.Controllers;

[Route("api/account")]
[ApiController]
public sealed class AccountController(
    UserManager<AppUser> userManager,
    ITokenService tokenService,
    SignInManager<AppUser> signInManager) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<NewUserDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Login(
        [FromBody] LoginDto loginDto,
        CancellationToken cancellationToken)
    {
        var identifier = loginDto.Username.Trim().ToUpperInvariant();
        var user = await userManager.Users.FirstOrDefaultAsync(
            candidate =>
                candidate.IsActive &&
                ((candidate.NormalizedUserName != null &&
                  candidate.NormalizedUserName == identifier) ||
                 (candidate.NormalizedEmail != null &&
                  candidate.NormalizedEmail == identifier)),
            cancellationToken);

        if (user is null)
        {
            return Unauthorized(
                ApiResponse<object>.Fail("Invalid username/email or password."));
        }

        var result = await signInManager.CheckPasswordSignInAsync(
            user,
            loginDto.Password,
            lockoutOnFailure: true);

        if (!result.Succeeded)
        {
            return Unauthorized(
                ApiResponse<object>.Fail("Invalid username/email or password."));
        }

        var response = await BuildUserAsync(user, includeToken: true);
        return Ok(ApiResponse<NewUserDto>.Ok(response, "Login successful."));
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<NewUserDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterDto dto,
        CancellationToken cancellationToken)
    {
        var username = dto.Username.Trim();
        var email = dto.Email.Trim();
        var normalizedUsername = username.ToUpperInvariant();
        var normalizedEmail = email.ToUpperInvariant();
        var exists = await userManager.Users.AnyAsync(
            user =>
                user.NormalizedUserName == normalizedUsername ||
                user.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (exists)
        {
            return Conflict(ApiResponse<object>.Fail("Username or email already exists."));
        }

        var user = new AppUser
        {
            UserName = username,
            Email = email,
            FullName = dto.FullName.Trim(),
            EmailConfirmed = true,
            IsActive = true
        };

        var created = await userManager.CreateAsync(user, dto.Password);
        if (!created.Succeeded)
        {
            return BadRequest(
                ApiResponse<object>.Fail("User creation failed.", Errors(created)));
        }

        // Public registration has no role input and is always least-privileged.
        var roleResult = await userManager.AddToRoleAsync(user, AppRoles.Developer);
        if (!roleResult.Succeeded)
        {
            await userManager.DeleteAsync(user);
            return StatusCode(
                StatusCodes.Status500InternalServerError,
                ApiResponse<object>.Fail("The account could not be completed."));
        }

        var response = await BuildUserAsync(user, includeToken: true);
        return CreatedAtAction(
            nameof(Me),
            null,
            ApiResponse<NewUserDto>.Ok(
                response,
                "User registered successfully."));
    }

    [Authorize]
    [HttpGet("me")]
    public async Task<IActionResult> Me()
    {
        var user = await userManager.FindByIdAsync(User.GetUserId());
        if (user is null)
        {
            return NotFound(ApiResponse<object>.Fail("User not found."));
        }

        var response = await BuildUserAsync(user, includeToken: false);
        return Ok(ApiResponse<NewUserDto>.Ok(response));
    }

    private async Task<NewUserDto> BuildUserAsync(
        AppUser user,
        bool includeToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new NewUserDto
        {
            Id = user.Id,
            UserName = user.UserName ?? string.Empty,
            Email = user.Email ?? string.Empty,
            FullName = user.FullName,
            Role = roles.FirstOrDefault() ?? AppRoles.Developer,
            Token = includeToken
                ? await tokenService.CreateToken(user)
                : string.Empty
        };
    }

    private static Dictionary<string, string[]> Errors(IdentityResult result) =>
        result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());
}
