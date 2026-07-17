using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Projects;

public sealed class CreateProjectDto
{
    [Required, StringLength(160, MinimumLength = 1)] public string Name { get; set; } = string.Empty;
    [StringLength(2000)] public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.UtcNow.Date;
    public DateTime? EndDate { get; set; }
}
