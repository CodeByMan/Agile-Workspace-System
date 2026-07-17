namespace api.Dtos.Projects;

public class ProjectDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public bool IsArchived { get; set; }
    public string CreatedByUserId { get; set; } = string.Empty;
    public string? CreatedByUserName { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
