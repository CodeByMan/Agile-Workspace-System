using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace api.Models;

[Table("SubTaskItems")]
public class SubTaskItem
{
    public int Id { get; set; }

    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    public bool IsCompleted { get; set; } = false;

    [Required]
    public int TaskItemId { get; set; }
    public TaskItem? TaskItem { get; set; }
}
