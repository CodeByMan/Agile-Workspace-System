namespace api.Dtos.Notifications;

public class NotificationDto
{
    public int Id { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string? Link { get; set; }
    public bool IsRead { get; set; }
    public int? TaskItemId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class NotificationSummaryDto
{
    public int Total { get; set; }
    public int Unread { get; set; }
    public List<NotificationDto> Items { get; set; } = new();
}
