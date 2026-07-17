using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

[Table("TaskAttachments")]
public class TaskAttachment
{
    public int Id { get; set; }

    [Required, MaxLength(260)]
    public string FileName { get; set; } = string.Empty;

    [Required, MaxLength(1000)]
    public string FileUrl { get; set; } = string.Empty;

    [MaxLength(100)]
    public string ContentType { get; set; } = string.Empty;

    public long FileSize { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    [Required]
    public string UploadedByUserId { get; set; } = string.Empty;
    public AppUser? UploadedByUser { get; set; }
}
