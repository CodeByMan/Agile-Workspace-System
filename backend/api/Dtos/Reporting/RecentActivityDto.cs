namespace api.Dtos.Reporting;

public class RecentActivityDto
{
    public int Id { get; set; }
    public int TaskId { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PerformedByUserId { get; set; } = string.Empty;
    public string? PerformedByUserName { get; set; }
    public string? TaskTitle { get; set; }
    public string? ProjectName { get; set; }
}
