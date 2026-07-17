using api.Models.Enums;
using AgileTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Dtos.Tasks;

public sealed class TaskQueryParameters
{
    public int? ProjectId { get; set; }
    public int? SprintId { get; set; }
    public bool? BacklogOnly { get; set; }
    public string? AssignedToUserId { get; set; }
    public AgileTaskStatus? Status { get; set; }
    public TaskPriority? Priority { get; set; }
    public WorkItemType? WorkItemType { get; set; }
    public string? Search { get; set; }
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    internal string? AccessibleByUserId { get; set; }
}
