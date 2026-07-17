using System.Security.Claims;
using api.Constants;
using api.Data;
using api.Dtos.Tasks;
using api.Interfaces;
using api.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace api.Tests;

public sealed class TransactionIntegrityTests : IClassFixture<AgileWorkspaceFactory>
{
    private readonly AgileWorkspaceFactory _factory;
    public TransactionIntegrityTests(AgileWorkspaceFactory factory) => _factory = factory;

    [Fact]
    public async Task FailedActivityInsert_RollsBackTaskCreation()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var (context, user, project) = await SeedProjectAsync(scope.ServiceProvider);
        var service = scope.ServiceProvider.GetRequiredService<ITaskService>();
        await context.Database.ExecuteSqlRawAsync("""
            CREATE TRIGGER fail_task_activity_insert
            BEFORE INSERT ON TaskActivityLogs
            BEGIN
                SELECT RAISE(ABORT, 'activity insert rejected');
            END;
            """);

        try
        {
            await Assert.ThrowsAsync<DbUpdateException>(() => service.CreateAsync(new CreateTaskDto
            {
                ProjectId = project.Id,
                Title = "Atomic work item"
            }, LeadershipUser(user.Id)));

            context.ChangeTracker.Clear();
            Assert.False(await context.TaskItems.AnyAsync(x => x.ProjectId == project.Id && x.Title == "Atomic work item"));
        }
        finally
        {
            await context.Database.ExecuteSqlRawAsync("DROP TRIGGER IF EXISTS fail_task_activity_insert;");
        }
    }

    [Fact]
    public async Task DailyUpdateUniqueIndex_RejectsDuplicateProjectUserDate()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var (context, user, project) = await SeedProjectAsync(scope.ServiceProvider);
        var date = DateTime.UtcNow.Date;
        context.DailyUpdates.Add(new DailyUpdate
        {
            ProjectId = project.Id,
            UserId = user.Id,
            UpdateDate = date,
            Yesterday = "Completed review",
            TodayPlan = "Implement changes",
            Blockers = string.Empty
        });
        await context.SaveChangesAsync();
        context.DailyUpdates.Add(new DailyUpdate
        {
            ProjectId = project.Id,
            UserId = user.Id,
            UpdateDate = date,
            Yesterday = "Duplicate",
            TodayPlan = "Duplicate",
            Blockers = string.Empty
        });

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task<(ApplicationDbContext Context, AppUser User, Project Project)> SeedProjectAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var users = services.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var user = new AppUser
        {
            UserName = $"atomic_{suffix}",
            Email = $"atomic_{suffix}@example.test",
            FullName = "Atomic Test User",
            EmailConfirmed = true,
            IsActive = true
        };
        Assert.True((await users.CreateAsync(user, "Strong!Pass123")).Succeeded);
        var project = new Project { Name = $"Atomic {suffix}", CreatedByUserId = user.Id, StartDate = DateTime.UtcNow.Date };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return (context, user, project);
    }

    private static ClaimsPrincipal LeadershipUser(string userId) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, userId),
        new Claim(ClaimTypes.Name, userId),
        new Claim(ClaimTypes.Role, AppRoles.TeamLead)
    ], "Test"));
}
