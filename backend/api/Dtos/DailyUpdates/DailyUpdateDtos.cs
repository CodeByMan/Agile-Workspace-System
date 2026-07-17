using System.ComponentModel.DataAnnotations;

namespace api.Dtos.DailyUpdates;

public sealed class CreateDailyUpdateDto
{
    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    [Range(1, int.MaxValue)]
    public int? SprintId { get; set; }

    [Required]
    [StringLength(1500, MinimumLength = 1)]
    public string Yesterday { get; set; } = string.Empty;

    [Required]
    [StringLength(1500, MinimumLength = 1)]
    public string TodayPlan { get; set; } = string.Empty;

    [StringLength(1500)]
    public string Blockers { get; set; } = string.Empty;

    public DateTime? UpdateDate { get; set; }
}

public sealed class DailyUpdateDto
{
    public int Id { get; set; }
    public int ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public int? SprintId { get; set; }
    public string? SprintName { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Yesterday { get; set; } = string.Empty;
    public string TodayPlan { get; set; } = string.Empty;
    public string Blockers { get; set; } = string.Empty;
    public DateTime UpdateDate { get; set; }
    public DateTime CreatedAt { get; set; }
}

public sealed class DailyUpdatePageDto
{
    public int TotalCount { get; set; }
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public List<DailyUpdateDto> Items { get; set; } = [];
}
