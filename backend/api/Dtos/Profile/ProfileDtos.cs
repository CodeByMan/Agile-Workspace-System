using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Profile;

public class ProfileDto
{
    public string Id { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public string? PhoneNumber { get; set; }
    public string Role { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateProfileDto
{
    [Required, StringLength(160, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(100)]
    public string? JobTitle { get; set; }

    [Phone, StringLength(50)]
    public string? PhoneNumber { get; set; }
}

public class ChangePasswordDto
{
    [Required]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string NewPassword { get; set; } = string.Empty;
}
