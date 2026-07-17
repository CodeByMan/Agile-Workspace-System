using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Notification
{
    public int Id { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }

    public int? TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    [Required, MaxLength(120)]
    public string Type { get; set; } = string.Empty;

    [Required, MaxLength(300)]
    public string Title { get; set; } = string.Empty;

    [Required, MaxLength(1500)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Link { get; set; }

    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReadAt { get; set; }
}
