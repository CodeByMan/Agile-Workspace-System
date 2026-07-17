using System.ComponentModel.DataAnnotations;
using api.Models.Enums;
using AgileTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Dtos.Tasks;

public class UpdateTaskDto
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(3000)]
    public string AcceptanceCriteria { get; set; } = string.Empty;

    public WorkItemType WorkItemType { get; set; } = WorkItemType.Task;
    public AgileTaskStatus Status { get; set; }
    public TaskPriority Priority { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }

    [Range(0, 100)]
    public int StoryPoints { get; set; }

    public bool IsRecurring { get; set; }
    [MaxLength(250)]
    public string? RecurrenceRule { get; set; }
    [Range(1, int.MaxValue)]
    public int? SprintId { get; set; }
    public string? AssignedToUserId { get; set; }
    [Range(1, int.MaxValue)]
    public int? ParentTaskId { get; set; }
    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
