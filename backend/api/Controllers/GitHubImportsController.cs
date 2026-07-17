using api.Constants;
using api.Dtos.Common;
using api.Dtos.Integrations;
using api.Extensions;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/integrations/github")]
[ApiController]
[Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager},{AppRoles.TeamLead}")]
public class GitHubImportsController : ControllerBase
{
    private readonly GitHubIssueImportService _service;

    public GitHubImportsController(GitHubIssueImportService service)
    {
        _service = service;
    }

    [HttpPost("issues/import")]
    public async Task<IActionResult> ImportIssues([FromBody] GitHubIssueImportRequest request, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var result = await _service.ImportAsync(request, User.GetUserId(), cancellationToken);
        return Ok(ApiResponse<GitHubIssueImportResult>.Ok(result, "GitHub issues imported successfully."));
    }
}
