using System.Net;
using System.Text;
using api.Data;
using api.Dtos.Integrations;
using api.Exceptions;
using api.Models;
using api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace api.Tests;

public sealed class GitHubImportServiceTests : IClassFixture<AgileWorkspaceFactory>
{
    private readonly AgileWorkspaceFactory _factory;
    public GitHubImportServiceTests(AgileWorkspaceFactory factory) => _factory = factory;

    [Fact]
    public async Task Import_MapsFields_ExcludesPullRequests_AndFollowsPagination()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var (context, user, project) = await SeedProjectAsync(scope.ServiceProvider);
        var handler = new QueueHandler(
            Response(HttpStatusCode.OK, """
            [
              {"title":"Issue One","body":"First body","html_url":"https://github.test/issues/1","labels":[{"name":"bug"}]},
              {"title":"Pull Request","body":"Not an issue","html_url":"https://github.test/pull/2","labels":[],"pull_request":{"url":"https://api.github.test/pulls/2"}}
            ]
            """, "<https://api.github.test/repos/acme/work/issues?state=open&per_page=100&page=2>; rel=\"next\""),
            Response(HttpStatusCode.OK, """
            [{"title":"Issue Two","body":null,"html_url":"https://github.test/issues/3","labels":[]}]
            """));
        var service = CreateService(context, handler);

        var result = await service.ImportAsync(new GitHubIssueImportRequest
        {
            ProjectId = project.Id,
            RepositoryOwner = " acme ",
            RepositoryName = " work ",
            ImportOpenIssuesOnly = true
        }, user.Id, CancellationToken.None);

        Assert.Equal(2, result.ImportedCount);
        Assert.Equal(0, result.SkippedCount);
        Assert.Equal(new[] { "Issue One", "Issue Two" }, result.ImportedTitles);
        var titles = await context.TaskItems.Where(x => x.ProjectId == project.Id).OrderBy(x => x.Title).Select(x => x.Title).ToListAsync();
        Assert.Equal(new[] { "Issue One", "Issue Two" }, titles);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Import_SkipsCaseInsensitiveDuplicateTitles()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var (context, user, project) = await SeedProjectAsync(scope.ServiceProvider);
        context.TaskItems.Add(new TaskItem { ProjectId = project.Id, Title = "Existing Issue", CreatedByUserId = user.Id });
        await context.SaveChangesAsync();
        var service = CreateService(context, new QueueHandler(Response(HttpStatusCode.OK,
            "[{\"title\":\"existing issue\",\"body\":\"duplicate\",\"html_url\":\"https://github.test/issues/4\",\"labels\":[]}]")));

        var result = await service.ImportAsync(new GitHubIssueImportRequest
        {
            ProjectId = project.Id,
            RepositoryOwner = "acme",
            RepositoryName = "work"
        }, user.Id, CancellationToken.None);

        Assert.Equal(0, result.ImportedCount);
        Assert.Equal(1, result.SkippedCount);
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound, typeof(NotFoundException))]
    [InlineData(HttpStatusCode.Unauthorized, typeof(ExternalServiceException))]
    [InlineData(HttpStatusCode.Forbidden, typeof(ExternalServiceException))]
    public async Task Import_MapsGitHubFailuresToSafeApiErrors(HttpStatusCode status, Type exceptionType)
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var (context, user, project) = await SeedProjectAsync(scope.ServiceProvider);
        var service = CreateService(context, new QueueHandler(Response(status, "{}")));

        var exception = await Record.ExceptionAsync(() => service.ImportAsync(new GitHubIssueImportRequest
        {
            ProjectId = project.Id,
            RepositoryOwner = "acme",
            RepositoryName = "work"
        }, user.Id, CancellationToken.None));

        Assert.NotNull(exception);
        Assert.Equal(exceptionType, exception.GetType());
    }

    [Fact]
    public async Task Import_RejectsMalformedGitHubResponseWithoutPartialPersistence()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var (context, user, project) = await SeedProjectAsync(scope.ServiceProvider);
        var service = CreateService(context, new QueueHandler(Response(HttpStatusCode.OK, "not-json")));

        await Assert.ThrowsAsync<ExternalServiceException>(() => service.ImportAsync(new GitHubIssueImportRequest
        {
            ProjectId = project.Id,
            RepositoryOwner = "acme",
            RepositoryName = "work"
        }, user.Id, CancellationToken.None));

        Assert.False(await context.TaskItems.AnyAsync(x => x.ProjectId == project.Id));
    }

    private static GitHubIssueImportService CreateService(ApplicationDbContext context, HttpMessageHandler handler)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["GitHub:BaseUrl"] = "https://api.github.test/",
            ["GitHub:MaxPages"] = "5"
        }).Build();
        return new GitHubIssueImportService(new HttpClient(handler), configuration, context);
    }

    private static async Task<(ApplicationDbContext Context, AppUser User, Project Project)> SeedProjectAsync(IServiceProvider services)
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var users = services.GetRequiredService<UserManager<AppUser>>();
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var user = new AppUser
        {
            UserName = $"github_{suffix}",
            Email = $"github_{suffix}@example.test",
            FullName = "GitHub Import User",
            EmailConfirmed = true,
            IsActive = true
        };
        Assert.True((await users.CreateAsync(user, "Strong!Pass123")).Succeeded);
        var project = new Project { Name = $"GitHub {suffix}", CreatedByUserId = user.Id, StartDate = DateTime.UtcNow.Date };
        context.Projects.Add(project);
        await context.SaveChangesAsync();
        return (context, user, project);
    }

    private static HttpResponseMessage Response(HttpStatusCode status, string json, string? link = null)
    {
        var response = new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
        if (link is not null) response.Headers.TryAddWithoutValidation("Link", link);
        return response;
    }

    private sealed class QueueHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new(responses);
        public List<Uri> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request.RequestUri ?? throw new InvalidOperationException("Request URI was missing."));
            if (_responses.Count == 0) throw new InvalidOperationException("No mocked GitHub response remains.");
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
