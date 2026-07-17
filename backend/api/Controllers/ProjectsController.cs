using api.Constants;
using api.Dtos.Common;
using api.Dtos.Projects;
using api.Extensions;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class ProjectsController(IProjectService projectService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<ProjectDto>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var projects = await projectService.GetAllAsync(User, cancellationToken);
        return Ok(ApiResponse<List<ProjectDto>>.Ok(projects));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken cancellationToken)
    {
        var project = await projectService.GetByIdAsync(id, User, cancellationToken);
        return project is null
            ? NotFound(ApiResponse<object>.Fail("Project not found."))
            : Ok(ApiResponse<ProjectDto>.Ok(project));
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status201Created)]
    public async Task<IActionResult> Create([FromBody] CreateProjectDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var created = await projectService.CreateAsync(dto, User.GetUserId(), cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            ApiResponse<ProjectDto>.Ok(created, "Project created successfully."));
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProjectDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return ValidationProblem(ModelState);
        }

        var updated = await projectService.UpdateAsync(id, dto, cancellationToken);
        return updated is null
            ? NotFound(ApiResponse<object>.Fail("Project not found."))
            : Ok(ApiResponse<ProjectDto>.Ok(updated, "Project updated successfully."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = AppRoles.Admin)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        var deleted = await projectService.DeleteAsync(id, cancellationToken);
        return deleted
            ? Ok(ApiResponse<object>.Ok(null, "Project deleted successfully."))
            : NotFound(ApiResponse<object>.Fail("Project not found."));
    }
}
