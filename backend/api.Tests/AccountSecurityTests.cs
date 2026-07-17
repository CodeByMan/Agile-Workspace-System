using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using api.Constants;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace api.Tests;

public sealed class AccountSecurityTests : IClassFixture<AgileWorkspaceFactory>
{
    private readonly AgileWorkspaceFactory _factory;
    public AccountSecurityTests(AgileWorkspaceFactory factory) => _factory = factory;

    [Fact]
    public async Task AnonymousRegistration_ForcesDeveloper_EvenWhenRoleIsInjected()
    {
        var client = _factory.CreateClient();
        var username = Unique("public");
        var response = await client.PostAsJsonAsync("/api/account/register", new
        {
            Username = username,
            Email = $"{username}@example.test",
            FullName = "Public User",
            Password = "Strong!Pass123",
            Role = AppRoles.Admin
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal(AppRoles.Developer, json.RootElement.GetProperty("Data").GetProperty("Role").GetString());

        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var user = await users.FindByNameAsync(username);
        Assert.NotNull(user);
        var roles = await users.GetRolesAsync(user!);
        Assert.Single(roles);
        Assert.Equal(AppRoles.Developer, roles[0]);
    }

    [Fact]
    public async Task OldToken_IsRejected_AfterAccountDeactivation()
    {
        var session = await RegisterAsync("status");
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByNameAsync(session.Username) ?? throw new InvalidOperationException();
            user.IsActive = false;
            Assert.True((await users.UpdateAsync(user)).Succeeded);
            Assert.True((await users.UpdateSecurityStampAsync(user)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeAsync(session.Token)).StatusCode);
    }

    [Fact]
    public async Task OldToken_IsRejected_AfterPasswordChange()
    {
        var session = await RegisterAsync("password");
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByNameAsync(session.Username) ?? throw new InvalidOperationException();
            Assert.True((await users.ChangePasswordAsync(user, "Strong!Pass123", "Changed!Pass123")).Succeeded);
            Assert.True((await users.UpdateSecurityStampAsync(user)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeAsync(session.Token)).StatusCode);
    }

    [Fact]
    public async Task OldToken_IsRejected_AfterRoleChange()
    {
        var session = await RegisterAsync("role");
        await using (var scope = _factory.Services.CreateAsyncScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
            var user = await users.FindByNameAsync(session.Username) ?? throw new InvalidOperationException();
            Assert.True((await users.RemoveFromRoleAsync(user, AppRoles.Developer)).Succeeded);
            Assert.True((await users.AddToRoleAsync(user, AppRoles.TeamLead)).Succeeded);
            Assert.True((await users.UpdateSecurityStampAsync(user)).Succeeded);
        }
        Assert.Equal(HttpStatusCode.Unauthorized, (await GetMeAsync(session.Token)).StatusCode);
    }

    private async Task<(string Username, string Token)> RegisterAsync(string prefix)
    {
        var username = Unique(prefix);
        var client = _factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/account/register", new
        {
            Username = username,
            Email = $"{username}@example.test",
            FullName = "Security Test User",
            Password = "Strong!Pass123"
        });
        response.EnsureSuccessStatusCode();
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return (username, json.RootElement.GetProperty("Data").GetProperty("Token").GetString()!);
    }

    private async Task<HttpResponseMessage> GetMeAsync(string token)
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.GetAsync("/api/account/me");
    }

    private static string Unique(string prefix) => $"{prefix}_{Guid.NewGuid():N}"[..24];
}
