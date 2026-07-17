using api.Dtos.Common;
using api.Dtos.Notifications;
using api.Extensions;
using api.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers;

[Route("api/notifications")]
[ApiController]
[Authorize]
public sealed class NotificationsController(INotificationService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetMyNotifications(
        [FromQuery] int take = 25,
        CancellationToken cancellationToken = default)
    {
        var notifications = await service.GetForUserAsync(
            User.GetUserId(),
            take,
            cancellationToken);

        return Ok(ApiResponse<NotificationSummaryDto>.Ok(notifications));
    }

    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(
        int id,
        CancellationToken cancellationToken = default)
    {
        var found = await service.MarkAsReadAsync(
            id,
            User.GetUserId(),
            cancellationToken);

        return found
            ? Ok(ApiResponse<object>.Ok(null, "Notification marked as read."))
            : NotFound(ApiResponse<object>.Fail("Notification not found."));
    }

    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken cancellationToken = default)
    {
        var count = await service.MarkAllAsReadAsync(
            User.GetUserId(),
            cancellationToken);

        return Ok(ApiResponse<object>.Ok(
            new { updated = count },
            "All notifications marked as read."));
    }
}
