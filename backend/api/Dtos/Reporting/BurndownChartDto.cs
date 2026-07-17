namespace api.Dtos.Reporting;

public class BurndownChartDto
{
    public int ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int InitialTaskCount { get; set; }
    public int InitialStoryPoints { get; set; }
    public List<BurndownPointDto> Points { get; set; } = new();
}
