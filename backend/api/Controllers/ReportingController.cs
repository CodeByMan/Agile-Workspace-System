using api.Constants;
using api.Dtos.Common;
using api.Dtos.Reporting;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class ReportingController : ControllerBase
{
    private readonly IReportingService _reportingService;

    public ReportingController(IReportingService reportingService)
    {
        _reportingService = reportingService;
    }

    [HttpGet("dashboard")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager},{AppRoles.TeamLead},{AppRoles.Developer}")]
    public async Task<IActionResult> GetDashboard(CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetDashboardSummaryAsync(User, cancellationToken);
        return Ok(ApiResponse<DashboardSummaryDto>.Ok(result));
    }

    [HttpGet("projects")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager},{AppRoles.TeamLead},{AppRoles.Developer}")]
    public async Task<IActionResult> GetProjectProgress(CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetProjectProgressAsync(User, cancellationToken);
        return Ok(ApiResponse<List<ProjectProgressDto>>.Ok(result));
    }

    [HttpGet("projects/{projectId:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager},{AppRoles.TeamLead},{AppRoles.Developer}")]
    public async Task<IActionResult> GetProjectProgressById(int projectId, CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetProjectProgressByIdAsync(projectId, User, cancellationToken);
        return result is null
            ? NotFound(ApiResponse<object>.Fail("Project analytics not found."))
            : Ok(ApiResponse<ProjectProgressDto>.Ok(result));
    }

    [HttpGet("users/productivity")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager},{AppRoles.TeamLead}")]
    public async Task<IActionResult> GetUserProductivity(CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetUserProductivityAsync(cancellationToken);
        return Ok(ApiResponse<List<UserProductivityDto>>.Ok(result));
    }

    [HttpGet("projects/{projectId:int}/burndown")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager},{AppRoles.TeamLead},{AppRoles.Developer}")]
    public async Task<IActionResult> GetBurndown(int projectId, CancellationToken cancellationToken)
    {
        var result = await _reportingService.GetBurndownAsync(projectId, User, cancellationToken);
        return result is null
            ? NotFound(ApiResponse<object>.Fail("Project burndown not found."))
            : Ok(ApiResponse<BurndownChartDto>.Ok(result));
    }

    [HttpGet("activity")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager},{AppRoles.TeamLead},{AppRoles.Developer}")]
    public async Task<IActionResult> GetRecentActivity([FromQuery] int take = 20, CancellationToken cancellationToken = default)
    {
        var result = await _reportingService.GetRecentActivityAsync(User, take, cancellationToken);
        return Ok(ApiResponse<List<RecentActivityDto>>.Ok(result));
    }
}
