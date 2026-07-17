namespace api.Dtos.Reporting;

public class DashboardSummaryDto
{
    public int TotalProjects { get; set; }
    public int ActiveProjects { get; set; }
    public int TotalTasks { get; set; }
    public int ToDoTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int DoneTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int DueThisWeekTasks { get; set; }
    public int CompletedThisWeekTasks { get; set; }
    public int TotalStoryPoints { get; set; }
    public int CompletedStoryPoints { get; set; }
    public decimal CompletionRate { get; set; }
    public List<TaskStatusCountDto> TasksByStatus { get; set; } = new();
    public List<ProjectProgressDto> TopProjects { get; set; } = new();
}
