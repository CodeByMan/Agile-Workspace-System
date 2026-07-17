using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using api.Models.Enums;
using AgileTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Models;

[Table("TaskItems")]
public class TaskItem
{
    public int Id { get; set; }
    [Required, MaxLength(200)] public string Title { get; set; } = string.Empty;
    [MaxLength(2000)] public string Description { get; set; } = string.Empty;
    [MaxLength(3000)] public string AcceptanceCriteria { get; set; } = string.Empty;
    public WorkItemType WorkItemType { get; set; } = WorkItemType.Task;
    public AgileTaskStatus Status { get; set; } = AgileTaskStatus.ToDo;
    public TaskPriority Priority { get; set; } = TaskPriority.Medium;
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int StoryPoints { get; set; }
    public bool IsRecurring { get; set; }
    [MaxLength(250)] public string? RecurrenceRule { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    [Required] public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public int? SprintId { get; set; }
    public Sprint? Sprint { get; set; }
    public string? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }
    [Required] public string CreatedByUserId { get; set; } = string.Empty;
    public AppUser? CreatedByUser { get; set; }
    public int? ParentTaskId { get; set; }
    public TaskItem? ParentTask { get; set; }

    public ICollection<TaskItem> ChildWorkItems { get; set; } = new List<TaskItem>();
    public ICollection<SubTaskItem> SubTasks { get; set; } = new List<SubTaskItem>();
    public ICollection<TaskComment> Comments { get; set; } = new List<TaskComment>();
    public ICollection<TaskActivityLog> ActivityLogs { get; set; } = new List<TaskActivityLog>();
    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();
}
