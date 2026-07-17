using api.Constants;
using api.Models;
using api.Models.Log;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace api.Data;

public class ApplicationDbContext(
    DbContextOptions<ApplicationDbContext> options)
    : IdentityDbContext<AppUser>(options)
{
    public DbSet<ApiLog> ApiLogs => Set<ApiLog>();
    public DbSet<ErrorLog> ErrorLogs => Set<ErrorLog>();
    public DbSet<Project> Projects => Set<Project>();
    public DbSet<Sprint> Sprints => Set<Sprint>();
    public DbSet<TaskItem> TaskItems => Set<TaskItem>();
    public DbSet<SubTaskItem> SubTaskItems => Set<SubTaskItem>();
    public DbSet<TaskComment> TaskComments => Set<TaskComment>();
    public DbSet<TaskActivityLog> TaskActivityLogs => Set<TaskActivityLog>();
    public DbSet<TaskAttachment> TaskAttachments => Set<TaskAttachment>();
    public DbSet<DailyUpdate> DailyUpdates => Set<DailyUpdate>();
    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        ConfigureUsers(builder);
        ConfigureProjects(builder);
        ConfigureSprints(builder);
        ConfigureTasks(builder);
        ConfigureTaskChildren(builder);
        ConfigureDailyUpdates(builder);
        ConfigureNotifications(builder);
        ConfigureLogs(builder);
        SeedRoles(builder);
    }

    private static void ConfigureUsers(ModelBuilder builder)
    {
        builder.Entity<AppUser>(entity =>
        {
            entity.Property(user => user.FullName).HasMaxLength(160).IsRequired();
            entity.Property(user => user.JobTitle).HasMaxLength(100);
            entity.Property(user => user.PhoneNumber).HasMaxLength(50);
        });
    }

    private static void ConfigureProjects(ModelBuilder builder)
    {
        builder.Entity<Project>(entity =>
        {
            entity.HasIndex(project => project.Name);
            entity.Property(project => project.RowVersion).IsRowVersion();
            entity
                .HasOne(project => project.CreatedByUser)
                .WithMany(user => user.CreatedProjects)
                .HasForeignKey(project => project.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureSprints(ModelBuilder builder)
    {
        builder.Entity<Sprint>(entity =>
        {
            entity.Property(sprint => sprint.RowVersion).IsRowVersion();
            entity
                .HasOne(sprint => sprint.Project)
                .WithMany(project => project.Sprints)
                .HasForeignKey(sprint => sprint.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(sprint => sprint.CreatedByUser)
                .WithMany(user => user.CreatedSprints)
                .HasForeignKey(sprint => sprint.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTasks(ModelBuilder builder)
    {
        builder.Entity<TaskItem>(entity =>
        {
            entity.Property(task => task.RowVersion).IsRowVersion();
            entity.HasIndex(task => new { task.ProjectId, task.Status });
            entity.HasIndex(task => new { task.AssignedToUserId, task.DueDate });
            entity
                .HasOne(task => task.Project)
                .WithMany(project => project.Tasks)
                .HasForeignKey(task => task.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(task => task.Sprint)
                .WithMany(sprint => sprint.WorkItems)
                .HasForeignKey(task => task.SprintId)
                .OnDelete(DeleteBehavior.NoAction);
            entity
                .HasOne(task => task.AssignedToUser)
                .WithMany(user => user.AssignedTasks)
                .HasForeignKey(task => task.AssignedToUserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity
                .HasOne(task => task.CreatedByUser)
                .WithMany(user => user.CreatedTasks)
                .HasForeignKey(task => task.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(task => task.ParentTask)
                .WithMany(task => task.ChildWorkItems)
                .HasForeignKey(task => task.ParentTaskId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureTaskChildren(ModelBuilder builder)
    {
        builder.Entity<SubTaskItem>()
            .HasOne(subTask => subTask.TaskItem)
            .WithMany(task => task.SubTasks)
            .HasForeignKey(subTask => subTask.TaskItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<TaskComment>(entity =>
        {
            entity
                .HasOne(comment => comment.TaskItem)
                .WithMany(task => task.Comments)
                .HasForeignKey(comment => comment.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(comment => comment.CreatedByUser)
                .WithMany(user => user.TaskComments)
                .HasForeignKey(comment => comment.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TaskActivityLog>(entity =>
        {
            entity
                .HasOne(log => log.TaskItem)
                .WithMany(task => task.ActivityLogs)
                .HasForeignKey(log => log.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(log => log.PerformedByUser)
                .WithMany(user => user.ActivityLogs)
                .HasForeignKey(log => log.PerformedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        builder.Entity<TaskAttachment>(entity =>
        {
            entity
                .HasOne(attachment => attachment.TaskItem)
                .WithMany(task => task.Attachments)
                .HasForeignKey(attachment => attachment.TaskItemId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(attachment => attachment.UploadedByUser)
                .WithMany(user => user.UploadedAttachments)
                .HasForeignKey(attachment => attachment.UploadedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureDailyUpdates(ModelBuilder builder)
    {
        builder.Entity<DailyUpdate>(entity =>
        {
            entity.Property(update => update.Yesterday).HasMaxLength(1500).IsRequired();
            entity.Property(update => update.TodayPlan).HasMaxLength(1500).IsRequired();
            entity.Property(update => update.Blockers).HasMaxLength(1500).IsRequired();
            entity.HasIndex(update => new { update.ProjectId, update.UpdateDate });
            entity.HasIndex(update => new { update.UserId, update.UpdateDate });
            entity
                .HasIndex(update => new
                {
                    update.ProjectId,
                    update.UserId,
                    update.UpdateDate
                })
                .IsUnique();
            entity
                .HasOne(update => update.Project)
                .WithMany(project => project.DailyUpdates)
                .HasForeignKey(update => update.ProjectId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(update => update.User)
                .WithMany(user => user.DailyUpdates)
                .HasForeignKey(update => update.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            entity
                .HasOne(update => update.Sprint)
                .WithMany(sprint => sprint.DailyUpdates)
                .HasForeignKey(update => update.SprintId)
                .OnDelete(DeleteBehavior.NoAction);
        });
    }

    private static void ConfigureNotifications(ModelBuilder builder)
    {
        builder.Entity<Notification>(entity =>
        {
            entity.HasIndex(notification => new
            {
                notification.UserId,
                notification.IsRead,
                notification.CreatedAt
            });
            entity
                .HasOne(notification => notification.User)
                .WithMany(user => user.Notifications)
                .HasForeignKey(notification => notification.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity
                .HasOne(notification => notification.TaskItem)
                .WithMany()
                .HasForeignKey(notification => notification.TaskItemId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static void ConfigureLogs(ModelBuilder builder)
    {
        builder.Entity<ApiLog>(entity =>
        {
            entity.Property(log => log.Uri).HasMaxLength(500);
            entity.Property(log => log.HttpMethod).HasMaxLength(16);
            entity.Property(log => log.RequestData).HasMaxLength(4096);
            entity.Property(log => log.ResponseData).HasMaxLength(4096);
        });

        builder.Entity<ErrorLog>(entity =>
        {
            entity.Property(log => log.Uri).HasMaxLength(500);
            entity.Property(log => log.HttpMethod).HasMaxLength(16);
            entity.Property(log => log.ErrorType).HasMaxLength(120);
            entity.Property(log => log.ErrorMessage).HasMaxLength(2000);
        });
    }

    private static void SeedRoles(ModelBuilder builder)
    {
        builder.Entity<IdentityRole>().HasData(
            AppRoles.All.Select(role => new IdentityRole
            {
                Id = role,
                Name = role,
                NormalizedName = role.ToUpperInvariant(),
                ConcurrencyStamp = $"role-{role.ToLowerInvariant()}"
            }));

        builder.Entity<IdentityRoleClaim<string>>().HasData(
            RoleClaim(-1, AppRoles.Admin, "projects.manage"),
            RoleClaim(-2, AppRoles.Admin, "users.manage"),
            RoleClaim(-3, AppRoles.Admin, "tasks.assign"),
            RoleClaim(-4, AppRoles.ScrumMaster, "projects.manage"),
            RoleClaim(-5, AppRoles.ScrumMaster, "tasks.assign"),
            RoleClaim(-6, AppRoles.ScrumMaster, "sprints.manage"),
            RoleClaim(-7, AppRoles.Manager, "projects.manage"),
            RoleClaim(-8, AppRoles.Manager, "tasks.assign"),
            RoleClaim(-9, AppRoles.TeamLead, "tasks.assign"),
            RoleClaim(-10, AppRoles.TeamLead, "daily-updates.review"),
            RoleClaim(-11, AppRoles.Developer, "tasks.update.own"),
            RoleClaim(-12, AppRoles.Developer, "daily-updates.submit"));
    }

    private static IdentityRoleClaim<string> RoleClaim(
        int id,
        string roleId,
        string claimValue) =>
        new()
        {
            Id = id,
            RoleId = roleId,
            ClaimType = "Permission",
            ClaimValue = claimValue
        };
}

