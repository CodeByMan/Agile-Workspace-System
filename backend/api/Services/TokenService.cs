using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace api.Services;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;
    private readonly UserManager<AppUser> _userManager;
    private readonly SymmetricSecurityKey _key;

    public TokenService(IConfiguration configuration, UserManager<AppUser> userManager)
    {
        _configuration = configuration;
        _userManager = userManager;
        var value = configuration["JWT:SigningKey"] ?? throw new InvalidOperationException("JWT signing key is missing.");
        _key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(value));
    }

    public async Task<string> CreateToken(AppUser user)
    {
        if (!user.IsActive) throw new InvalidOperationException("Inactive users cannot receive authentication tokens.");
        var securityStamp = user.SecurityStamp ?? await _userManager.GetSecurityStampAsync(user);
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? string.Empty),
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new("fullName", user.FullName),
            new("security_stamp", securityStamp)
        };
        claims.AddRange((await _userManager.GetRolesAsync(user)).Select(role => new Claim(ClaimTypes.Role, role)));

        var lifetimeMinutes = Math.Clamp(_configuration.GetValue("JWT:LifetimeMinutes", 60), 5, 240);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(lifetimeMinutes),
            SigningCredentials = new SigningCredentials(_key, SecurityAlgorithms.HmacSha512),
            Issuer = _configuration["JWT:Issuer"],
            Audience = _configuration["JWT:Audience"]
        };
        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
