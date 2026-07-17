using System.Security.Claims;
using api.Constants;
using api.Dtos.Sprints;
using api.Models;
using api.Services;
using Microsoft.EntityFrameworkCore;

namespace api.Tests;

public sealed class SprintServiceTests
{
    [Fact]
    public async Task CreateAndUpdateUseServiceLayerAndPreserveConcurrencyTokenContract()
    {
        await using var context = TestDb.CreateInMemory();
        context.Projects.Add(new Project
        {
            Id = 10,
            Name = "Portfolio Project",
            CreatedByUserId = "manager-1",
            StartDate = DateTime.UtcNow.Date
        });
        await context.SaveChangesAsync();

        var service = new SprintService(
            context,
            new ResourceAuthorizationService(context));
        var sprintId = await service.CreateAsync(
            new CreateSprintDto
            {
                Name = "Sprint 1",
                Goal = "Deliver the project board",
                ProjectId = 10,
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(14)
            },
            "manager-1");

        var created = await context.Sprints
            .AsNoTracking()
            .SingleAsync(x => x.Id == sprintId);
        var result = await service.UpdateAsync(
            sprintId,
            new UpdateSprintDto
            {
                Name = "Sprint 1 - Updated",
                Goal = created.Goal,
                StartDate = created.StartDate,
                EndDate = created.EndDate,
                IsClosed = false,
                RowVersion = Convert.ToBase64String([1])
            });

        Assert.NotNull(result);
        Assert.Equal(sprintId, result.Id);
        Assert.Equal("Sprint 1 - Updated", (await context.Sprints.FindAsync(sprintId))!.Name);
    }

    [Fact]
    public async Task DeveloperOnlyReceivesSprintsForAccessibleProjects()
    {
        await using var context = TestDb.CreateInMemory();
        context.Projects.AddRange(
            new Project
            {
                Id = 1,
                Name = "Accessible",
                CreatedByUserId = "developer-1",
                StartDate = DateTime.UtcNow.Date
            },
            new Project
            {
                Id = 2,
                Name = "Hidden",
                CreatedByUserId = "other-user",
                StartDate = DateTime.UtcNow.Date
            });
        context.Sprints.AddRange(
            new Sprint
            {
                Id = 1,
                Name = "Visible Sprint",
                ProjectId = 1,
                CreatedByUserId = "developer-1",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(7)
            },
            new Sprint
            {
                Id = 2,
                Name = "Hidden Sprint",
                ProjectId = 2,
                CreatedByUserId = "other-user",
                StartDate = DateTime.UtcNow.Date,
                EndDate = DateTime.UtcNow.Date.AddDays(7)
            });
        await context.SaveChangesAsync();

        var service = new SprintService(
            context,
            new ResourceAuthorizationService(context));
        var result = await service.GetAllAsync(
            null,
            User("developer-1", AppRoles.Developer));

        var sprint = Assert.Single(result);
        Assert.Equal("Visible Sprint", sprint.Name);
    }

    private static ClaimsPrincipal User(string id, string role) =>
        new(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Name, id),
            new Claim(ClaimTypes.Role, role)
        ], "Test"));
}
