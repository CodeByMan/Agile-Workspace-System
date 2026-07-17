using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class Sprint
{
    public int Id { get; set; }

    [Required, MaxLength(160)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1500)]
    public string Goal { get; set; } = string.Empty;

    public int ProjectId { get; set; }
    public Project? Project { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }

    [Timestamp]
    public byte[] RowVersion { get; set; } = [];

    [Required]
    public string CreatedByUserId { get; set; } = string.Empty;
    public AppUser? CreatedByUser { get; set; }

    public ICollection<TaskItem> WorkItems { get; set; } = new List<TaskItem>();
    public ICollection<DailyUpdate> DailyUpdates { get; set; } = new List<DailyUpdate>();
}
