using System.Security.Claims;
using api.Constants;
using api.Dtos.Tasks;
using api.Exceptions;
using api.Models;
using api.Models.Enums;
using api.Services;
using AgileTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Tests;

public sealed class ResourceAuthorizationTests
{
    [Fact]
    public async Task DeveloperCannotAccessArbitraryTaskById()
    {
        await using var context = TestDb.CreateInMemory();
        var service = new ResourceAuthorizationService(context);
        var task = new TaskItem
        {
            Title = "Protected",
            ProjectId = 1,
            CreatedByUserId = "owner",
            AssignedToUserId = "assignee"
        };

        Assert.False(await service.CanAccessTaskAsync(User("outsider", AppRoles.Developer), task));
        Assert.True(await service.CanAccessTaskAsync(User("assignee", AppRoles.Developer), task));
        Assert.True(await service.CanAccessTaskAsync(User("owner", AppRoles.Developer), task));
    }

    [Theory]
    [InlineData(AppRoles.Developer, false)]
    [InlineData(AppRoles.TeamLead, true)]
    [InlineData(AppRoles.Manager, true)]
    [InlineData(AppRoles.ScrumMaster, true)]
    [InlineData(AppRoles.Admin, true)]
    public async Task ProjectRealtimeGroupRequiresLeadership(string role, bool expected)
    {
        await using var context = TestDb.CreateInMemory();
        context.Projects.Add(new Project
        {
            Id = 25,
            Name = "Realtime",
            CreatedByUserId = "owner",
            StartDate = DateTime.UtcNow.Date
        });
        await context.SaveChangesAsync();

        var actual = await new ResourceAuthorizationService(context)
            .CanJoinProjectRealtimeAsync(User("member", role), 25);

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void DeveloperMayUpdateContentAndStatusOfAssignedTask()
    {
        var task = ExistingTask();
        var update = MatchingUpdate(task);
        update.Title = "Clarified title";
        update.Description = "Updated implementation notes";
        update.AcceptanceCriteria = "Updated acceptance criteria";
        update.Status = AgileTaskStatus.InProgress;

        ResourceAuthorizationService.EnsureTaskUpdateAllowed(
            User("developer", AppRoles.Developer),
            task,
            update);
    }

    [Theory]
    [InlineData("assignee")]
    [InlineData("sprint")]
    [InlineData("parent")]
    [InlineData("recurrence")]
    [InlineData("type")]
    [InlineData("priority")]
    [InlineData("estimate")]
    [InlineData("schedule")]
    public void DeveloperCannotChangePlanningOrAssignmentFields(string field)
    {
        var task = ExistingTask();
        var update = MatchingUpdate(task);

        switch (field)
        {
            case "assignee":
                update.AssignedToUserId = "another-user";
                break;
            case "sprint":
                update.SprintId = 99;
                break;
            case "parent":
                update.ParentTaskId = 77;
                break;
            case "recurrence":
                update.IsRecurring = true;
                update.RecurrenceRule = "FREQ=WEEKLY";
                break;
            case "type":
                update.WorkItemType = WorkItemType.Bug;
                break;
            case "priority":
                update.Priority = TaskPriority.Critical;
                break;
            case "estimate":
                update.StoryPoints = 13;
                break;
            case "schedule":
                update.DueDate = task.DueDate?.AddDays(1);
                break;
        }

        Assert.Throws<ForbiddenException>(() =>
            ResourceAuthorizationService.EnsureTaskUpdateAllowed(
                User("developer", AppRoles.Developer),
                task,
                update));
    }


    [Theory]
    [InlineData(AppRoles.Developer)]
    [InlineData(AppRoles.TeamLead)]
    public void NonDeletingRolesCannotDeleteWorkItems(string role)
    {
        Assert.Throws<ForbiddenException>(() =>
            ResourceAuthorizationService.EnsureTaskDeletionAllowed(User("actor", role)));
    }

    [Theory]
    [InlineData(AppRoles.Manager)]
    [InlineData(AppRoles.ScrumMaster)]
    [InlineData(AppRoles.Admin)]
    public void AuthorizedManagementRolesMayDeleteWorkItems(string role)
    {
        ResourceAuthorizationService.EnsureTaskDeletionAllowed(User("actor", role));
    }

    [Fact]
    public void LeadershipMayChangePlanningFields()
    {
        var task = ExistingTask();
        var update = MatchingUpdate(task);
        update.AssignedToUserId = "another-user";
        update.SprintId = 99;
        update.Priority = TaskPriority.High;

        ResourceAuthorizationService.EnsureTaskUpdateAllowed(
            User("lead", AppRoles.TeamLead),
            task,
            update);
    }

    [Fact]
    public void DeveloperCannotUpdateAnotherDevelopersTask()
    {
        var task = ExistingTask();
        task.AssignedToUserId = "another-user";
        task.CreatedByUserId = "creator";

        Assert.Throws<ForbiddenException>(() =>
            ResourceAuthorizationService.EnsureTaskUpdateAllowed(
                User("developer", AppRoles.Developer),
                task,
                MatchingUpdate(task)));
    }

    private static TaskItem ExistingTask() => new()
    {
        Id = 12,
        Title = "Implement profile form",
        Description = "Current description",
        AcceptanceCriteria = "Current acceptance criteria",
        ProjectId = 1,
        SprintId = 5,
        AssignedToUserId = "developer",
        CreatedByUserId = "lead",
        WorkItemType = WorkItemType.Task,
        Status = AgileTaskStatus.ToDo,
        Priority = TaskPriority.Medium,
        StoryPoints = 5,
        StartDate = new DateTime(2026, 7, 1),
        DueDate = new DateTime(2026, 7, 10),
        IsRecurring = false,
        RecurrenceRule = null
    };

    private static UpdateTaskDto MatchingUpdate(TaskItem task) => new()
    {
        Title = task.Title,
        Description = task.Description,
        AcceptanceCriteria = task.AcceptanceCriteria,
        WorkItemType = task.WorkItemType,
        Status = task.Status,
        Priority = task.Priority,
        StoryPoints = task.StoryPoints,
        StartDate = task.StartDate,
        DueDate = task.DueDate,
        IsRecurring = task.IsRecurring,
        RecurrenceRule = task.RecurrenceRule,
        SprintId = task.SprintId,
        AssignedToUserId = task.AssignedToUserId,
        ParentTaskId = task.ParentTaskId,
        RowVersion = Convert.ToBase64String(task.RowVersion)
    };

    private static ClaimsPrincipal User(string id, string role) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, id),
        new Claim(ClaimTypes.Name, id),
        new Claim(ClaimTypes.Role, role)
    ], "Test"));
}
