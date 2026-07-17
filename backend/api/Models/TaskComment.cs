using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

[Table("TaskComments")]
public class TaskComment
{
    public int Id { get; set; }

    [Required, MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public int TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;
    public AppUser? CreatedByUser { get; set; }
}
