using api.Constants;
using api.Dtos.Common;
using api.Dtos.Tasks;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public sealed class TasksController(ITaskService taskService) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] TaskQueryParameters query,
        CancellationToken cancellationToken)
    {
        var result = await taskService.GetFilteredAsync(query, User, cancellationToken);
        return Ok(ApiResponse<object>.Ok(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(
        int id,
        CancellationToken cancellationToken)
    {
        var task = await taskService.GetByIdAsync(id, User, cancellationToken);
        return task is null
            ? NotFound(ApiResponse<object>.Fail("Work item not found."))
            : Ok(ApiResponse<TaskDto>.Ok(task));
    }

    [HttpPost]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager},{AppRoles.TeamLead}")]
    public async Task<IActionResult> Create(
        [FromBody] CreateTaskDto dto,
        CancellationToken cancellationToken)
    {
        var created = await taskService.CreateAsync(dto, User, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = created.Id },
            ApiResponse<TaskDto>.Ok(created, "Work item created successfully."));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(
        int id,
        [FromBody] UpdateTaskDto dto,
        CancellationToken cancellationToken)
    {
        var updated = await taskService.UpdateAsync(id, dto, User, cancellationToken);
        return updated is null
            ? NotFound(ApiResponse<object>.Fail("Work item not found."))
            : Ok(ApiResponse<TaskDto>.Ok(updated, "Work item updated successfully."));
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = $"{AppRoles.Admin},{AppRoles.ScrumMaster},{AppRoles.Manager}")]
    public async Task<IActionResult> Delete(
        int id,
        CancellationToken cancellationToken)
    {
        var deleted = await taskService.DeleteAsync(id, User, cancellationToken);
        return deleted
            ? Ok(ApiResponse<object>.Ok(null, "Work item deleted successfully."))
            : NotFound(ApiResponse<object>.Fail("Work item not found."));
    }

    [HttpPost("{id:int}/comments")]
    public async Task<IActionResult> AddComment(
        int id,
        [FromBody] CreateTaskCommentDto dto,
        CancellationToken cancellationToken)
    {
        var comment = await taskService.AddCommentAsync(id, dto, User, cancellationToken);
        return comment is null
            ? NotFound(ApiResponse<object>.Fail("Work item not found."))
            : Ok(ApiResponse<TaskCommentDto>.Ok(
                comment,
                "Comment added successfully."));
    }
}
