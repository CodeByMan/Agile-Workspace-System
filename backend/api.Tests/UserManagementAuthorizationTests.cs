using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using api.Constants;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace api.Tests;

public sealed class UserManagementAuthorizationTests : IClassFixture<AgileWorkspaceFactory>
{
    private readonly AgileWorkspaceFactory _factory;
    public UserManagementAuthorizationTests(AgileWorkspaceFactory factory) => _factory = factory;

    public static TheoryData<string> NonAdminRoles => new()
    {
        AppRoles.ScrumMaster,
        AppRoles.Manager,
        AppRoles.TeamLead,
        AppRoles.Developer
    };

    [Theory]
    [MemberData(nameof(NonAdminRoles))]
    public async Task EveryNonAdminRole_IsForbiddenFromCreatingAdmin(string actorRole)
    {
        var actor = await CreateSessionAsync(actorRole);
        var response = await actor.Client.PostAsJsonAsync("/api/users", NewManagedUser(AppRoles.Admin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminMayCreateAdminThroughAdministrativeEndpoint()
    {
        var actor = await CreateSessionAsync(AppRoles.Admin);
        var response = await actor.Client.PostAsJsonAsync("/api/users", NewManagedUser(AppRoles.Admin));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task TeamLeadCannotPromoteSelf()
    {
        var actor = await CreateSessionAsync(AppRoles.TeamLead);
        var response = await actor.Client.PutAsJsonAsync($"/api/users/{actor.User.Id}", new
        {
            FullName = actor.User.FullName,
            Email = actor.User.Email,
            Role = AppRoles.Manager,
            JobTitle = "Team Lead",
            PhoneNumber = "+1 555 0100"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Theory]
    [MemberData(nameof(NonAdminRoles))]
    public async Task EveryNonAdminRole_IsForbiddenFromDeactivatingAdmin(string actorRole)
    {
        var admin = await CreateUserAsync(AppRoles.Admin);
        var actor = await CreateSessionAsync(actorRole);
        var response = await actor.Client.PatchAsync($"/api/users/{admin.Id}/status?isActive=false", null);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<(HttpClient Client, AppUser User)> CreateSessionAsync(string role)
    {
        var user = await CreateUserAsync(role);
        await using var scope = _factory.Services.CreateAsyncScope();
        var tokens = scope.ServiceProvider.GetRequiredService<ITokenService>();
        var token = await tokens.CreateToken(user);
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return (client, user);
    }

    private async Task<AppUser> CreateUserAsync(string role)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var user = new AppUser
        {
            UserName = $"actor_{suffix}",
            Email = $"actor_{suffix}@example.test",
            FullName = "Authorization Test User",
            EmailConfirmed = true,
            IsActive = true
        };
        Assert.True((await users.CreateAsync(user, "Strong!Pass123")).Succeeded);
        Assert.True((await users.AddToRoleAsync(user, role)).Succeeded);
        return user;
    }

    private static object NewManagedUser(string role)
    {
        var suffix = Guid.NewGuid().ToString("N")[..12];
        return new
        {
            UserName = $"managed_{suffix}",
            Email = $"managed_{suffix}@example.test",
            FullName = "Managed Test User",
            Password = "Strong!Pass123",
            Role = role,
            JobTitle = "Test",
            PhoneNumber = "+1 555 0101"
        };
    }
}
