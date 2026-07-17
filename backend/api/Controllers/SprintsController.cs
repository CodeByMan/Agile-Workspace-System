using api.Constants;
using api.Dtos.Common;
using api.Dtos.Sprints;
using api.Extensions;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class SprintsController(ISprintService sprintService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int? projectId,
        CancellationToken cancellationToken)
    {
        var sprints = await sprintService.GetAllAsync(projectId, User, cancellationToken);
        return Ok(ApiResponse<IReadOnlyList<SprintDto>>.Ok(sprints));
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager}")]
    public async Task<IActionResult> Create(
        [FromBody] CreateSprintDto dto,
        CancellationToken cancellationToken)
    {
        var id = await sprintService.CreateAsync(dto, User.GetUserId(), cancellationToken);
        return CreatedAtAction(
            nameof(GetAll),
            new { projectId = dto.ProjectId },
            ApiResponse<object>.Ok(new { Id = id }, "Sprint created successfully."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateSprintDto dto,
        CancellationToken cancellationToken)
    {
        var result = await sprintService.UpdateAsync(id, dto, cancellationToken);
        return result is null
            ? NotFound(ApiResponse<object>.Fail("Sprint not found."))
            : Ok(ApiResponse<object>.Ok(
                new { Id = result.Id, RowVersion = result.RowVersion },
                "Sprint updated successfully."));
    }
}
