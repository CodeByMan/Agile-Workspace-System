using api.Constants;
using api.Models;
using api.Models.Enums;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WorkTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Data;

public static class DemoDataSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext context,
        UserManager<AppUser> userManager,
        IConfiguration configuration)
    {
        if (await context.Projects.AnyAsync())
        {
            return;
        }

        var admin = await EnsureDemoUserAsync(
            userManager,
            configuration,
            "Admin",
            "admin",
            "admin@agileworkspace.local",
            "System Administrator",
            AppRoles.Admin);
        var scrumMaster = await EnsureDemoUserAsync(
            userManager,
            configuration,
            "ScrumMaster",
            "scrummaster",
            "scrum@agileworkspace.local",
            "Scrum Master",
            AppRoles.ScrumMaster);
        var manager = await EnsureDemoUserAsync(
            userManager,
            configuration,
            "Manager",
            "manager",
            "manager@agileworkspace.local",
            "Delivery Manager",
            AppRoles.Manager);
        var teamLead = await EnsureDemoUserAsync(
            userManager,
            configuration,
            "TeamLead",
            "teamlead",
            "lead@agileworkspace.local",
            "Engineering Lead",
            AppRoles.TeamLead);
        var developer = await EnsureDemoUserAsync(
            userManager,
            configuration,
            "Developer",
            "developer",
            "developer@agileworkspace.local",
            "Full Stack Developer",
            AppRoles.Developer);

        var today = DateTime.UtcNow.Date;
        var atlas = new Project
        {
            Name = "Atlas Commerce Delivery",
            Description = "Checkout modernization workspace for backlog refinement, release planning, and sprint execution.",
            StartDate = today.AddDays(-45),
            CreatedByUserId = manager.Id
        };
        var portal = new Project
        {
            Name = "Customer Portal Modernization",
            Description = "Self-service portal redesign covering profile management, ticket visibility, and notification workflows.",
            StartDate = today.AddDays(-30),
            CreatedByUserId = scrumMaster.Id
        };
        var workspace = new Project
        {
            Name = "Agile Workspace Platform",
            Description = "Internal engineering platform used to coordinate sprint planning, task delivery, and collaboration.",
            StartDate = today.AddDays(-20),
            CreatedByUserId = admin.Id
        };

        await context.Projects.AddRangeAsync(atlas, portal, workspace);
        await context.SaveChangesAsync();

        var sprints = new List<Sprint>
        {
            new()
            {
                Name = "Atlas Sprint 1",
                Goal = "Stabilize shipping and payment analytics.",
                ProjectId = atlas.Id,
                StartDate = today.AddDays(-28),
                EndDate = today.AddDays(-14),
                IsClosed = true,
                CreatedByUserId = scrumMaster.Id
            },
            new()
            {
                Name = "Atlas Sprint 2",
                Goal = "Complete payment recovery and duplicate submission fixes.",
                ProjectId = atlas.Id,
                StartDate = today.AddDays(-13),
                EndDate = today.AddDays(1),
                IsClosed = false,
                CreatedByUserId = scrumMaster.Id
            },
            new()
            {
                Name = "Atlas Sprint 3",
                Goal = "Prepare release hardening and post-launch monitoring.",
                ProjectId = atlas.Id,
                StartDate = today.AddDays(2),
                EndDate = today.AddDays(16),
                IsClosed = false,
                CreatedByUserId = scrumMaster.Id
            },
            new()
            {
                Name = "Portal Sprint 1",
                Goal = "Launch profile experience improvements.",
                ProjectId = portal.Id,
                StartDate = today.AddDays(-21),
                EndDate = today.AddDays(-7),
                IsClosed = true,
                CreatedByUserId = scrumMaster.Id
            },
            new()
            {
                Name = "Portal Sprint 2",
                Goal = "Deliver secure notifications and search experience.",
                ProjectId = portal.Id,
                StartDate = today.AddDays(-6),
                EndDate = today.AddDays(8),
                IsClosed = false,
                CreatedByUserId = scrumMaster.Id
            },
            new()
            {
                Name = "Workspace Sprint 1",
                Goal = "Ship reporting and collaboration features.",
                ProjectId = workspace.Id,
                StartDate = today.AddDays(-10),
                EndDate = today.AddDays(4),
                IsClosed = false,
                CreatedByUserId = admin.Id
            }
        };

        await context.Sprints.AddRangeAsync(sprints);
        await context.SaveChangesAsync();
        var sprintByName = sprints.ToDictionary(x => x.Name, x => x);

        var tasks = new List<TaskItem>
        {
            new()
            {
                Title = "Improve checkout conversion",
                Description = "Epic tracking the primary checkout improvement stream.",
                ProjectId = atlas.Id,
                SprintId = sprintByName["Atlas Sprint 2"].Id,
                CreatedByUserId = manager.Id,
                AssignedToUserId = teamLead.Id,
                Status = WorkTaskStatus.InProgress,
                Priority = TaskPriority.High,
                WorkItemType = WorkItemType.Epic,
                StartDate = today.AddDays(-13),
                DueDate = today.AddDays(10),
                StoryPoints = 13
            },
            new()
            {
                Title = "Capture shipping step drop-off analytics",
                Description = "Instrument all checkout exits and publish metrics.",
                ProjectId = atlas.Id,
                SprintId = sprintByName["Atlas Sprint 1"].Id,
                CreatedByUserId = manager.Id,
                AssignedToUserId = developer.Id,
                Status = WorkTaskStatus.Done,
                Priority = TaskPriority.High,
                WorkItemType = WorkItemType.UserStory,
                StartDate = today.AddDays(-24),
                DueDate = today.AddDays(-17),
                CompletedAt = today.AddDays(-16),
                StoryPoints = 5
            },
            new()
            {
                Title = "Streamline payment error handling",
                Description = "Preserve form data and improve user recovery options.",
                ProjectId = atlas.Id,
                SprintId = sprintByName["Atlas Sprint 2"].Id,
                CreatedByUserId = teamLead.Id,
                AssignedToUserId = developer.Id,
                Status = WorkTaskStatus.InProgress,
                Priority = TaskPriority.High,
                WorkItemType = WorkItemType.UserStory,
                StartDate = today.AddDays(-8),
                DueDate = today.AddDays(3),
                StoryPoints = 8
            },
            new()
            {
                Title = "Fix duplicate order submission race condition",
                Description = "Prevent duplicate payment requests caused by repeated button clicks.",
                ProjectId = atlas.Id,
                SprintId = sprintByName["Atlas Sprint 2"].Id,
                CreatedByUserId = admin.Id,
                AssignedToUserId = developer.Id,
                Status = WorkTaskStatus.ToDo,
                Priority = TaskPriority.Critical,
                WorkItemType = WorkItemType.Bug,
                StartDate = today,
                DueDate = today.AddDays(2),
                StoryPoints = 3
            },
            new()
            {
                Title = "Design backlog for release readiness",
                Description = "Collect release hardening tasks, smoke tests, and observability needs.",
                ProjectId = atlas.Id,
                CreatedByUserId = scrumMaster.Id,
                AssignedToUserId = teamLead.Id,
                Status = WorkTaskStatus.ToDo,
                Priority = TaskPriority.Medium,
                WorkItemType = WorkItemType.Task,
                DueDate = today.AddDays(12),
                StoryPoints = 2
            },
            new()
            {
                Title = "Customer profile redesign",
                Description = "Epic covering the profile view, edit, and password change journey.",
                ProjectId = portal.Id,
                SprintId = sprintByName["Portal Sprint 2"].Id,
                CreatedByUserId = scrumMaster.Id,
                AssignedToUserId = teamLead.Id,
                Status = WorkTaskStatus.InProgress,
                Priority = TaskPriority.High,
                WorkItemType = WorkItemType.Epic,
                StartDate = today.AddDays(-6),
                DueDate = today.AddDays(8),
                StoryPoints = 13
            },
            new()
            {
                Title = "Add profile update API and UI",
                Description = "Deliver editable profile details with validation and feedback.",
                ProjectId = portal.Id,
                SprintId = sprintByName["Portal Sprint 2"].Id,
                CreatedByUserId = teamLead.Id,
                AssignedToUserId = developer.Id,
                Status = WorkTaskStatus.InProgress,
                Priority = TaskPriority.High,
                WorkItemType = WorkItemType.UserStory,
                StartDate = today.AddDays(-4),
                DueDate = today.AddDays(5),
                StoryPoints = 5
            },
            new()
            {
                Title = "Implement notification center",
                Description = "Show unread notifications, mark them read, and support action links.",
                ProjectId = portal.Id,
                SprintId = sprintByName["Portal Sprint 2"].Id,
                CreatedByUserId = manager.Id,
                AssignedToUserId = developer.Id,
                Status = WorkTaskStatus.ToDo,
                Priority = TaskPriority.Medium,
                WorkItemType = WorkItemType.UserStory,
                DueDate = today.AddDays(6),
                StoryPoints = 5
            },
            new()
            {
                Title = "Backlog triage for self-service search",
                Description = "Refine search stories for a future sprint.",
                ProjectId = portal.Id,
                CreatedByUserId = scrumMaster.Id,
                AssignedToUserId = teamLead.Id,
                Status = WorkTaskStatus.ToDo,
                Priority = TaskPriority.Low,
                WorkItemType = WorkItemType.Task,
                DueDate = today.AddDays(15),
                StoryPoints = 2
            },
            new()
            {
                Title = "Reporting dashboard v1",
                Description = "Ship API summaries and visual dashboard widgets.",
                ProjectId = workspace.Id,
                SprintId = sprintByName["Workspace Sprint 1"].Id,
                CreatedByUserId = admin.Id,
                AssignedToUserId = teamLead.Id,
                Status = WorkTaskStatus.InProgress,
                Priority = TaskPriority.High,
                WorkItemType = WorkItemType.UserStory,
                StartDate = today.AddDays(-10),
                DueDate = today.AddDays(2),
                StoryPoints = 8
            },
            new()
            {
                Title = "SignalR server broadcast channel",
                Description = "Broadcast committed work-item updates for a future authenticated Angular client.",
                ProjectId = workspace.Id,
                SprintId = sprintByName["Workspace Sprint 1"].Id,
                CreatedByUserId = admin.Id,
                AssignedToUserId = developer.Id,
                Status = WorkTaskStatus.Done,
                Priority = TaskPriority.Medium,
                WorkItemType = WorkItemType.Task,
                StartDate = today.AddDays(-9),
                DueDate = today.AddDays(-2),
                CompletedAt = today.AddDays(-1),
                StoryPoints = 3
            },
            new()
            {
                Title = "Seed realistic demo data",
                Description = "Populate users, projects, sprints, tasks, updates, and notifications.",
                ProjectId = workspace.Id,
                SprintId = sprintByName["Workspace Sprint 1"].Id,
                CreatedByUserId = admin.Id,
                AssignedToUserId = developer.Id,
                Status = WorkTaskStatus.InProgress,
                Priority = TaskPriority.Medium,
                WorkItemType = WorkItemType.Task,
                StartDate = today.AddDays(-5),
                DueDate = today.AddDays(3),
                StoryPoints = 3
            },
            new()
            {
                Title = "Prepare Docker deployment assets",
                Description = "Add portfolio Docker configuration for the API and Angular application.",
                ProjectId = workspace.Id,
                CreatedByUserId = admin.Id,
                AssignedToUserId = teamLead.Id,
                Status = WorkTaskStatus.ToDo,
                Priority = TaskPriority.Low,
                WorkItemType = WorkItemType.Task,
                DueDate = today.AddDays(9),
                StoryPoints = 2
            }
        };

        await context.TaskItems.AddRangeAsync(tasks);
        await context.SaveChangesAsync();

        var now = DateTime.UtcNow;
        await context.TaskComments.AddRangeAsync(
            new TaskComment
            {
                TaskItemId = tasks[2].Id,
                CreatedByUserId = manager.Id,
                Content = "Keep the payment retry message short and actionable."
            },
            new TaskComment
            {
                TaskItemId = tasks[6].Id,
                CreatedByUserId = teamLead.Id,
                Content = "Profile edits should save optimistically but still surface validation errors."
            },
            new TaskComment
            {
                TaskItemId = tasks[9].Id,
                CreatedByUserId = admin.Id,
                Content = "Use agile-focused delivery metrics, not generic business metrics."
            });

        await context.TaskActivityLogs.AddRangeAsync(
            new TaskActivityLog
            {
                TaskItemId = tasks[1].Id,
                PerformedByUserId = developer.Id,
                ActivityType = "StatusChanged",
                Description = "Completed analytics instrumentation and shared results.",
                CreatedAt = now.AddDays(-16)
            },
            new TaskActivityLog
            {
                TaskItemId = tasks[2].Id,
                PerformedByUserId = teamLead.Id,
                ActivityType = "StatusChanged",
                Description = "Moved payment recovery work into active development.",
                CreatedAt = now.AddDays(-7)
            },
            new TaskActivityLog
            {
                TaskItemId = tasks[3].Id,
                PerformedByUserId = admin.Id,
                ActivityType = "RiskRaised",
                Description = "Logged a critical release blocker related to duplicate order submissions.",
                CreatedAt = now.AddHours(-20)
            },
            new TaskActivityLog
            {
                TaskItemId = tasks[6].Id,
                PerformedByUserId = developer.Id,
                ActivityType = "Implementation",
                Description = "Profile update form wired to the profile API.",
                CreatedAt = now.AddHours(-8)
            },
            new TaskActivityLog
            {
                TaskItemId = tasks[9].Id,
                PerformedByUserId = teamLead.Id,
                ActivityType = "DashboardUpdated",
                Description = "Reporting widgets updated to show project delivery health.",
                CreatedAt = now.AddHours(-3)
            });

        await context.DailyUpdates.AddRangeAsync(
            new DailyUpdate
            {
                ProjectId = atlas.Id,
                SprintId = sprintByName["Atlas Sprint 2"].Id,
                UserId = developer.Id,
                Yesterday = "Completed payment retry UX edge cases.",
                TodayPlan = "Finish duplicate order submission fix and support QA.",
                Blockers = "Waiting for staging payment gateway credentials.",
                UpdateDate = today,
                CreatedAt = now.AddHours(-2)
            },
            new DailyUpdate
            {
                ProjectId = portal.Id,
                SprintId = sprintByName["Portal Sprint 2"].Id,
                UserId = teamLead.Id,
                Yesterday = "Reviewed profile management API contract with the frontend.",
                TodayPlan = "Finalize notifications integration and verify responsive states.",
                Blockers = string.Empty,
                UpdateDate = today,
                CreatedAt = now.AddHours(-1)
            },
            new DailyUpdate
            {
                ProjectId = workspace.Id,
                SprintId = sprintByName["Workspace Sprint 1"].Id,
                UserId = developer.Id,
                Yesterday = "Replaced dashboard placeholders with real reporting calls.",
                TodayPlan = "Polish backlog and sprint management views.",
                Blockers = string.Empty,
                UpdateDate = today,
                CreatedAt = now.AddMinutes(-40)
            });

        await context.Notifications.AddRangeAsync(
            new Notification
            {
                UserId = teamLead.Id,
                Type = "task-assigned",
                Title = "New work item assigned",
                Message = "Design backlog for release readiness was assigned to you.",
                Link = "/backlog",
                TaskItemId = tasks[4].Id,
                CreatedAt = now.AddHours(-4),
                IsRead = false
            },
            new Notification
            {
                UserId = developer.Id,
                Type = "comment",
                Title = "Comment added",
                Message = "A new comment was added on Add profile update API and UI.",
                Link = "/backlog",
                TaskItemId = tasks[6].Id,
                CreatedAt = now.AddHours(-2),
                IsRead = false
            },
            new Notification
            {
                UserId = manager.Id,
                Type = "daily-update",
                Title = "New daily update submitted",
                Message = "Developer submitted a daily update for Atlas Commerce Delivery.",
                Link = "/sprints",
                CreatedAt = now.AddMinutes(-30),
                IsRead = false
            },
            new Notification
            {
                UserId = admin.Id,
                Type = "delivery",
                Title = "Dashboard refresh",
                Message = "Reporting dashboard v1 progress increased after latest task updates.",
                Link = "/dashboard",
                TaskItemId = tasks[9].Id,
                CreatedAt = now.AddMinutes(-15),
                IsRead = true,
                ReadAt = now.AddMinutes(-10)
            });

        await context.SaveChangesAsync();
    }

    private static async Task<AppUser> EnsureDemoUserAsync(
        UserManager<AppUser> userManager,
        IConfiguration configuration,
        string key,
        string defaultUserName,
        string defaultEmail,
        string jobTitle,
        string role)
    {
        var password = configuration[$"Seed:Users:{key}:Password"];
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidOperationException(
                $"Demo mode is enabled but Seed:Users:{key}:Password is missing. " +
                "Use .NET user secrets or environment variables.");
        }

        var userName = configuration[$"Seed:Users:{key}:Username"]?.Trim() ?? defaultUserName;
        var email = configuration[$"Seed:Users:{key}:Email"]?.Trim() ?? defaultEmail;
        var user = await userManager.FindByNameAsync(userName);
        if (user is null)
        {
            user = new AppUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                FullName = jobTitle,
                JobTitle = jobTitle,
                IsActive = true
            };

            var create = await userManager.CreateAsync(user, password);
            if (!create.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not create demo user '{userName}': " +
                    string.Join("; ", create.Errors.Select(x => x.Description)));
            }
        }

        var roles = await userManager.GetRolesAsync(user);
        if (!roles.Contains(role, StringComparer.OrdinalIgnoreCase))
        {
            var addRole = await userManager.AddToRoleAsync(user, role);
            if (!addRole.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Could not assign demo role '{role}' to '{userName}'.");
            }
        }

        return user;
    }
}
