using System.ComponentModel.DataAnnotations;
using api.Constants;

namespace api.Dtos.Users;

public sealed class UserSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public int AssignedTasks { get; set; }
    public int CompletedTasks { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class UserListResultDto
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public List<UserSummaryDto> Items { get; set; } = [];
}

public sealed class UpdateUserDto
{
    [Required, StringLength(160, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = AppRoles.Developer;

    [StringLength(100)]
    public string? JobTitle { get; set; }

    [Phone, StringLength(50)]
    public string? PhoneNumber { get; set; }
}

public sealed class CreateManagedUserDto
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string UserName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string Password { get; set; } = string.Empty;

    [Required]
    public string Role { get; set; } = AppRoles.Developer;

    [StringLength(100)]
    public string? JobTitle { get; set; }

    [Phone, StringLength(50)]
    public string? PhoneNumber { get; set; }
}
