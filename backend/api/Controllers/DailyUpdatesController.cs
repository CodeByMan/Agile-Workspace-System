using api.Dtos.Common;
using api.Dtos.DailyUpdates;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/daily-updates")]
[ApiController]
[Authorize]
public sealed class DailyUpdatesController(IDailyUpdateService dailyUpdateService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? projectId,
        [FromQuery] DateTime? date,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await dailyUpdateService.GetAllAsync(
            projectId,
            date,
            pageNumber,
            pageSize,
            User,
            cancellationToken);

        return Ok(ApiResponse<DailyUpdatePageDto>.Ok(result));
    }

    [HttpPost]
    public async Task<IActionResult> Create(
        [FromBody] CreateDailyUpdateDto dto,
        CancellationToken cancellationToken)
    {
        var result = await dailyUpdateService.CreateAsync(dto, User, cancellationToken);
        return CreatedAtAction(
            nameof(GetAll),
            new { projectId = result.ProjectId, date = result.UpdateDate },
            ApiResponse<object>.Ok(
                new { Id = result.Id },
                "Daily update submitted successfully."));
    }
}
