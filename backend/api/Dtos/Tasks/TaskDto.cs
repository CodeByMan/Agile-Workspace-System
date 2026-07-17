using api.Models.Enums;
using AgileTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Dtos.Tasks;

public class TaskDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public WorkItemType WorkItemType { get; set; }
    public AgileTaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? CompletedAt { get; set; }
    public int StoryPoints { get; set; }
    public bool IsRecurring { get; set; }
    public string? RecurrenceRule { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int? SprintId { get; set; }
    public string? SprintName { get; set; }
    public string? AssignedToUserId { get; set; }
    public string? AssignedToUserName { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? CreatedByUserName { get; set; }
    public int? ParentTaskId { get; set; }
    public int SubTaskCount { get; set; }
    public int CompletedSubTaskCount { get; set; }
    public string RowVersion { get; set; } = string.Empty;
    public List<TaskCommentDto> Comments { get; set; } = new();
    public List<TaskActivityLogDto> ActivityLogs { get; set; } = new();
}
