using System.Security.Claims;
using api.Dtos.Reporting;

namespace api.Interfaces;

public interface IReportingService
{
    Task<DashboardSummaryDto> GetDashboardSummaryAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<List<ProjectProgressDto>> GetProjectProgressAsync(
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<ProjectProgressDto?> GetProjectProgressByIdAsync(
        int projectId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<List<UserProductivityDto>> GetUserProductivityAsync(
        CancellationToken cancellationToken = default);

    Task<BurndownChartDto?> GetBurndownAsync(
        int projectId,
        ClaimsPrincipal user,
        CancellationToken cancellationToken = default);

    Task<List<RecentActivityDto>> GetRecentActivityAsync(
        ClaimsPrincipal user,
        int take = 20,
        CancellationToken cancellationToken = default);
}
