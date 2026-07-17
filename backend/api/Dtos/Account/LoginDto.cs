using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Account;

public class LoginDto
{
    [Required, StringLength(256, MinimumLength = 1)]
    public string Username { get; set; } = string.Empty;

    [Required, StringLength(128, MinimumLength = 1)]
    public string Password { get; set; } = string.Empty;
}
