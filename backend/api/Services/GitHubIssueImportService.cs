using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;
using api.Data;
using api.Dtos.Integrations;
using api.Exceptions;
using api.Models;
using api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using WorkTaskStatus = api.Models.Enums.TaskStatus;

namespace api.Services;

public sealed class GitHubIssueImportService(
    HttpClient httpClient,
    IConfiguration configuration,
    ApplicationDbContext context)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<GitHubIssueImportResult> ImportAsync(
        GitHubIssueImportRequest request,
        string userId,
        CancellationToken cancellationToken)
    {
        var owner = request.RepositoryOwner.Trim();
        var repository = request.RepositoryName.Trim();
        ValidateRepositoryName(owner, repository);

        if (!await context.Projects.AnyAsync(x => x.Id == request.ProjectId, cancellationToken))
        {
            throw new NotFoundException("Target project not found.");
        }

        if (request.SprintId.HasValue &&
            !await context.Sprints.AnyAsync(
                x => x.Id == request.SprintId && x.ProjectId == request.ProjectId,
                cancellationToken))
        {
            throw new BadRequestException("The selected sprint does not belong to the target project.");
        }

        var baseUri = GetConfiguredBaseUri();
        var maxPages = Math.Clamp(configuration.GetValue("GitHub:MaxPages", 10), 1, 50);
        var issues = await FetchIssuesAsync(
            baseUri,
            owner,
            repository,
            request.ImportOpenIssuesOnly,
            maxPages,
            cancellationToken);

        var existingTitles = new HashSet<string>(
            await context.TaskItems
                .AsNoTracking()
                .Where(x => x.ProjectId == request.ProjectId)
                .Select(x => x.Title)
                .ToListAsync(cancellationToken),
            StringComparer.OrdinalIgnoreCase);

        var candidates = issues
            .Where(x => x.PullRequest is null && !string.IsNullOrWhiteSpace(x.Title))
            .ToList();

        var result = new GitHubIssueImportResult { Repository = $"{owner}/{repository}" };
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            foreach (var issue in candidates)
            {
                var title = Truncate(issue.Title.Trim(), 200);
                if (!existingTitles.Add(title))
                {
                    result.SkippedCount++;
                    continue;
                }

                context.TaskItems.Add(new TaskItem
                {
                    ProjectId = request.ProjectId,
                    SprintId = request.SprintId,
                    Title = title,
                    Description = Truncate((issue.Body ?? string.Empty).Trim(), 2000),
                    AcceptanceCriteria = Truncate((issue.HtmlUrl ?? string.Empty).Trim(), 3000),
                    WorkItemType = (issue.Labels ?? [])
                        .Any(x => x.Name.Contains("bug", StringComparison.OrdinalIgnoreCase))
                            ? WorkItemType.Bug
                            : WorkItemType.UserStory,
                    Status = WorkTaskStatus.ToDo,
                    Priority = TaskPriority.Medium,
                    CreatedByUserId = userId,
                    StoryPoints = 0
                });

                result.ImportedCount++;
                result.ImportedTitles.Add(title);
            }

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }

        return result;
    }

    private async Task<List<GitHubIssueDto>> FetchIssuesAsync(
        Uri baseUri,
        string owner,
        string repository,
        bool openIssuesOnly,
        int maxPages,
        CancellationToken cancellationToken)
    {
        var issues = new List<GitHubIssueDto>();
        Uri? next = new(
            baseUri,
            $"repos/{Uri.EscapeDataString(owner)}/{Uri.EscapeDataString(repository)}/issues" +
            $"?state={(openIssuesOnly ? "open" : "all")}&per_page=100&page=1");
        var pages = 0;

        while (next is not null)
        {
            if (++pages > maxPages)
            {
                throw new ExternalServiceException(
                    "GitHub response exceeded the configured pagination limit.");
            }

            using var message = CreateRequest(next);
            using var response = await httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken);

            EnsureSuccessfulResponse(response);
            issues.AddRange(await ReadIssuePageAsync(response, cancellationToken));
            next = GetNextLink(response.Headers, baseUri);
        }

        return issues;
    }

    private HttpRequestMessage CreateRequest(Uri uri)
    {
        var message = new HttpRequestMessage(HttpMethod.Get, uri);
        message.Headers.UserAgent.Add(new ProductInfoHeaderValue("AgileWorkspace", "1.0"));
        message.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

        var token = configuration["GitHub:PersonalAccessToken"];
        if (!string.IsNullOrWhiteSpace(token))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Trim());
        }

        return message;
    }

    private static void EnsureSuccessfulResponse(HttpResponseMessage response)
    {
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            throw new NotFoundException("GitHub repository was not found or is not accessible.");
        }

        if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
        {
            var rateLimited = response.Headers.TryGetValues(
                                  "X-RateLimit-Remaining",
                                  out var remaining) &&
                              remaining.FirstOrDefault() == "0";
            throw new ExternalServiceException(
                rateLimited
                    ? "GitHub rate limit was exceeded."
                    : "GitHub authentication failed or repository access was denied.",
                StatusCodes.Status502BadGateway);
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new ExternalServiceException(
                $"GitHub returned status {(int)response.StatusCode}.",
                StatusCodes.Status502BadGateway);
        }
    }

    private static async Task<List<GitHubIssueDto>> ReadIssuePageAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            return await JsonSerializer.DeserializeAsync<List<GitHubIssueDto>>(
                       stream,
                       JsonOptions,
                       cancellationToken)
                   ?? throw new JsonException("The response did not contain an issue collection.");
        }
        catch (JsonException)
        {
            throw new ExternalServiceException("GitHub returned a malformed issue response.");
        }
    }

    private Uri GetConfiguredBaseUri()
    {
        var baseUrl = (configuration["GitHub:BaseUrl"] ?? "https://api.github.com/")
            .TrimEnd('/') + "/";
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
        {
            throw new BadRequestException("GitHub base URL configuration is invalid.");
        }

        if (!string.Equals(baseUri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
            !baseUri.IsLoopback)
        {
            throw new BadRequestException(
                "GitHub base URL must use HTTPS unless it targets a loopback development endpoint.");
        }

        return baseUri;
    }

    private static void ValidateRepositoryName(string owner, string repository)
    {
        if (owner.Length == 0 || repository.Length == 0 || owner.Contains('/') || repository.Contains('/'))
        {
            throw new BadRequestException(
                "Repository owner and name are required and must not contain slashes.");
        }
    }

    private static string Truncate(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value[..maximumLength];

    private static Uri? GetNextLink(HttpResponseHeaders headers, Uri baseUri)
    {
        if (!headers.TryGetValues("Link", out var values))
        {
            return null;
        }

        foreach (var segment in string.Join(',', values).Split(','))
        {
            var parts = segment.Split(';', StringSplitOptions.TrimEntries);
            if (parts.Length < 2 ||
                !parts.Skip(1).Any(x => x.Equals("rel=\"next\"", StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var value = parts[0].Trim().Trim('<', '>');
            if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
            {
                return HasSameOrigin(baseUri, absolute) ? absolute : null;
            }

            if (Uri.TryCreate(baseUri, value, out var relative))
            {
                return HasSameOrigin(baseUri, relative) ? relative : null;
            }
        }

        return null;
    }

    private static bool HasSameOrigin(Uri expected, Uri candidate) =>
        string.Equals(expected.Scheme, candidate.Scheme, StringComparison.OrdinalIgnoreCase) &&
        string.Equals(expected.Host, candidate.Host, StringComparison.OrdinalIgnoreCase) &&
        expected.Port == candidate.Port;

    private sealed class GitHubIssueDto
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string? Body { get; set; }

        [JsonPropertyName("html_url")]
        public string? HtmlUrl { get; set; }

        [JsonPropertyName("labels")]
        public List<GitHubLabelDto>? Labels { get; set; } = [];

        [JsonPropertyName("pull_request")]
        public JsonElement? PullRequest { get; set; }
    }

    private sealed class GitHubLabelDto
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }
}
