using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Account;

public sealed class RegisterDto
{
    [Required, StringLength(50, MinimumLength = 3)]
    public string Username { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(256)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(160, MinimumLength = 3)]
    public string FullName { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 10)]
    public string Password { get; set; } = string.Empty;
}
