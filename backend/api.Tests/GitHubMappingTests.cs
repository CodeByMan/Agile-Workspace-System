using System.Reflection;
using System.Text.Json;
using api.Services;

namespace api.Tests;

public sealed class GitHubMappingTests
{
    [Fact]
    public void RealisticGitHubJson_MapsSnakeCaseFields_AndPullRequestMarker()
    {
        const string json = """
        {
          "title": "Fix importer",
          "body": "Map GitHub fields explicitly",
          "html_url": "https://github.example/issues/1",
          "labels": [{ "name": "bug" }],
          "pull_request": { "url": "https://github.example/pulls/1" }
        }
        """;

        var dtoType = typeof(GitHubIssueImportService).GetNestedType("GitHubIssueDto", BindingFlags.NonPublic)!;
        var dto = JsonSerializer.Deserialize(json, dtoType, new JsonSerializerOptions(JsonSerializerDefaults.Web))!;

        Assert.Equal("Fix importer", dtoType.GetProperty("Title")!.GetValue(dto));
        Assert.Equal("Map GitHub fields explicitly", dtoType.GetProperty("Body")!.GetValue(dto));
        Assert.Equal("https://github.example/issues/1", dtoType.GetProperty("HtmlUrl")!.GetValue(dto));
        Assert.NotNull(dtoType.GetProperty("PullRequest")!.GetValue(dto));
    }

    [Fact]
    public void RegisterDto_DoesNotExposeRoleInput()
    {
        var roleProperty = typeof(api.Dtos.Account.RegisterDto).GetProperty("Role", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
        Assert.Null(roleProperty);
    }
    [Fact]
    public void PaginationLink_UsesNextRelation()
    {
        using var response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation("Link", "<https://api.github.test/repos/acme/work/issues?page=2>; rel=\"next\", <https://api.github.test/repos/acme/work/issues?page=4>; rel=\"last\"");
        var method = typeof(GitHubIssueImportService).GetMethod("GetNextLink", BindingFlags.NonPublic | BindingFlags.Static)!;

        var next = (Uri?)method.Invoke(null, [response.Headers, new Uri("https://api.github.test/")]);

        Assert.Equal("https://api.github.test/repos/acme/work/issues?page=2", next?.AbsoluteUri);
    }

    [Fact]
    public void UserSummaryContract_PreservesPhoneNumber()
    {
        Assert.NotNull(typeof(api.Dtos.Users.UserSummaryDto).GetProperty("PhoneNumber"));
    }

    [Fact]
    public void PaginationLink_RejectsDifferentOrigin()
    {
        using var response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation("Link", "<https://untrusted.example/issues?page=2>; rel=\"next\"");
        var method = typeof(GitHubIssueImportService).GetMethod("GetNextLink", BindingFlags.NonPublic | BindingFlags.Static)!;

        var next = (Uri?)method.Invoke(null, [response.Headers, new Uri("https://api.github.test/")]);

        Assert.Null(next);
    }

}
