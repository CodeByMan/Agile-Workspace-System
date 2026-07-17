using System.ComponentModel.DataAnnotations;

namespace api.Dtos.Integrations;

public class GitHubIssueImportRequest
{
    [Range(1, int.MaxValue)]
    public int ProjectId { get; set; }

    [Required, MaxLength(200)]
    public string RepositoryOwner { get; set; } = string.Empty;

    [Required, MaxLength(200)]
    public string RepositoryName { get; set; } = string.Empty;

    [Range(1, int.MaxValue)]
    public int? SprintId { get; set; }
    public bool ImportOpenIssuesOnly { get; set; } = true;
}

public class GitHubIssueImportResult
{
    public string Repository { get; set; } = string.Empty;
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> ImportedTitles { get; set; } = new();
}
