using api.Models.Log;
using Microsoft.AspNetCore.Identity;

namespace api.Models;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
    public string? JobTitle { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<ApiLog> ApiLogs { get; set; } = new List<ApiLog>();
    public ICollection<Project> CreatedProjects { get; set; } = new List<Project>();
    public ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    public ICollection<TaskComment> TaskComments { get; set; } = new List<TaskComment>();
    public ICollection<TaskActivityLog> ActivityLogs { get; set; } = new List<TaskActivityLog>();
    public ICollection<TaskAttachment> UploadedAttachments { get; set; } = new List<TaskAttachment>();
    public ICollection<Sprint> CreatedSprints { get; set; } = new List<Sprint>();
    public ICollection<DailyUpdate> DailyUpdates { get; set; } = new List<DailyUpdate>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
