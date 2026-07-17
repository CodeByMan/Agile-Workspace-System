using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

[Table("TaskActivityLogs")]
public class TaskActivityLog
{
    public int Id { get; set; }

    [Required, MaxLength(100)]
    public string ActivityType { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? OldValue { get; set; }

    [MaxLength(200)]
    public string? NewValue { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    [Required]
    public string PerformedByUserId { get; set; } = string.Empty;
    public AppUser? PerformedByUser { get; set; }
}
