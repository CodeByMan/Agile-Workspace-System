using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Project
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsArchived { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;
    public AppUser? CreatedByUser { get; set; }

    public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
    public ICollection<DailyUpdate> DailyUpdates { get; set; } = new List<DailyUpdate>();
}
