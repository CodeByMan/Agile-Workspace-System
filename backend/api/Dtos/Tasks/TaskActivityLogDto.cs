namespace api.Dtos.Tasks;

public class TaskActivityLogDto
{
    public int Id { get; set; }
    public string ActivityType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public string PerformedByUserId { get; set; } = string.Empty;
    public string? PerformedByUserName { get; set; }
}
