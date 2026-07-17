using System.Security.Claims;
using api.Constants;
using api.Dtos.Tasks;
using api.Exceptions;
using api.Hubs;
using api.Interfaces;
using api.Models;
using api.Models.Enums;
using api.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using AgileTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Tests;

public sealed class TaskValidationTests
{
    [Fact]
    public async Task CreateRejectsWhitespaceTitle()
    {
        await using var context = TestDb.CreateInMemory();
        var project = await AddProjectAsync(context);
        var service = CreateService(context);

        await Assert.ThrowsAsync<BadRequestException>(() => service.CreateAsync(
            new CreateTaskDto { ProjectId = project.Id, Title = "   " }, LeadershipUser()));
    }

    [Fact]
    public async Task CreateRejectsSprintFromAnotherProject()
    {
        await using var context = TestDb.CreateInMemory();
        var project = await AddProjectAsync(context);
        var otherProject = await AddProjectAsync(context, "Other");
        var sprint = new Sprint { Name = "Other Sprint", ProjectId = otherProject.Id, StartDate = DateTime.UtcNow.Date, EndDate = DateTime.UtcNow.Date.AddDays(7), CreatedByUserId = "leader" };
        context.Sprints.Add(sprint);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService(context).CreateAsync(
            new CreateTaskDto { ProjectId = project.Id, SprintId = sprint.Id, Title = "Valid title" }, LeadershipUser()));
    }

    [Fact]
    public async Task CreateRejectsInactiveAssignee()
    {
        await using var context = TestDb.CreateInMemory();
        var project = await AddProjectAsync(context);
        context.Users.Add(new AppUser { Id = "inactive", UserName = "inactive", Email = "inactive@example.test", IsActive = false });
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService(context).CreateAsync(
            new CreateTaskDto { ProjectId = project.Id, AssignedToUserId = "inactive", Title = "Valid title" }, LeadershipUser()));
    }

    [Fact]
    public async Task UpdateRejectsParentCycle()
    {
        await using var context = TestDb.CreateInMemory();
        var project = await AddProjectAsync(context);
        var current = new TaskItem { Title = "Current", ProjectId = project.Id, CreatedByUserId = "leader" };
        context.TaskItems.Add(current);
        await context.SaveChangesAsync();
        var proposedParent = new TaskItem { Title = "Child", ProjectId = project.Id, CreatedByUserId = "leader", ParentTaskId = current.Id };
        context.TaskItems.Add(proposedParent);
        await context.SaveChangesAsync();

        var dto = new UpdateTaskDto
        {
            Title = "Current",
            Status = AgileTaskStatus.ToDo,
            Priority = TaskPriority.Medium,
            WorkItemType = WorkItemType.Task,
            ParentTaskId = proposedParent.Id
        };

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService(context).UpdateAsync(current.Id, dto, LeadershipUser()));
    }

    [Theory]
    [InlineData(1, 0)]
    [InlineData(5, 4)]
    public async Task CreateRejectsInvalidDateRange(int startOffset, int dueOffset)
    {
        await using var context = TestDb.CreateInMemory();
        var project = await AddProjectAsync(context);
        var day = DateTime.UtcNow.Date;
        var dto = new CreateTaskDto { ProjectId = project.Id, Title = "Dates", StartDate = day.AddDays(startOffset), DueDate = day.AddDays(dueOffset) };

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService(context).CreateAsync(dto, LeadershipUser()));
    }

    [Fact]
    public async Task CreateRejectsUndefinedEnumValue()
    {
        await using var context = TestDb.CreateInMemory();
        var project = await AddProjectAsync(context);
        var dto = new CreateTaskDto { ProjectId = project.Id, Title = "Enums", Status = (AgileTaskStatus)999 };

        await Assert.ThrowsAsync<BadRequestException>(() => CreateService(context).CreateAsync(dto, LeadershipUser()));
    }

    private static TaskService CreateService(api.Data.ApplicationDbContext context) => new(
        Mock.Of<ITaskRepository>(),
        context,
        Mock.Of<IHubContext<TaskCollaborationHub>>(),
        new ResourceAuthorizationService(context),
        NullLogger<TaskService>.Instance);

    private static async Task<Project> AddProjectAsync(api.Data.ApplicationDbContext context, string name = "Project")
    {
        var project = new Project { Name = name, CreatedByUserId = "leader", StartDate = DateTime.UtcNow.Date };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return project;
    }

    private static ClaimsPrincipal LeadershipUser() => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, "leader"),
        new Claim(ClaimTypes.Name, "leader"),
        new Claim(ClaimTypes.Role, AppRoles.TeamLead)
    ], "Test"));
}
