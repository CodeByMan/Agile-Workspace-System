namespace api.Dtos.Reporting;

public class ProjectProgressDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public bool IsArchived { get; set; }
    public int TotalTasks { get; set; }
    public int ToDoTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int DoneTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int TotalStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public decimal CompletionRate { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
