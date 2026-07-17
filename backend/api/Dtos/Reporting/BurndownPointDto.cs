namespace api.Dtos.Reporting;

public class BurndownPointDto
{
    public DateTime Date { get; set; }
    public int RemainingTasks { get; set; }
    public int RemainingStoryPoints { get; set; }
    public int CompletedTasks { get; set; }
}
