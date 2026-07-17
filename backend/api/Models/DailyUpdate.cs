using System.ComponentModel.DataAnnotations;

namespace api.Models;

public class DailyUpdate
{
    public int Id { get; set; }

    [Required]
    public int ProjectId { get; set; }
    public Project? Project { get; set; }

    public int? SprintId { get; set; }
    public Sprint? Sprint { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;
    public AppUser? User { get; set; }

    [MaxLength(1500)]
    public string Yesterday { get; set; } = string.Empty;

    [MaxLength(1500)]
    public string TodayPlan { get; set; } = string.Empty;

    [MaxLength(1500)]
    public string Blockers { get; set; } = string.Empty;

    public DateTime UpdateDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
