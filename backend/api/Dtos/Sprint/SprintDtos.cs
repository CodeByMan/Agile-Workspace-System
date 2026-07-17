using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Sprints;

public sealed class SprintDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Goal { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public string? ProjectName { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }
    public int PlannedItemsCount { get; set; }
    public int CompletedItemsCount { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}

public sealed class CreateSprintDto
{
    [Required]
    [StringLength(160, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1500)]
    public string Goal { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
}

public sealed class UpdateSprintDto
{
    [Required]
    [StringLength(160, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [StringLength(1500)]
    public string Goal { get; set; } = string.Empty;

    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsClosed { get; set; }

    [Required]
    public string RowVersion { get; set; } = string.Empty;
}
