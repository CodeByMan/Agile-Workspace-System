using api.Dtos.Notifications;

namespace api.Interfaces;

public interface INotificationService
{
    Task<NotificationSummaryDto> GetForUserAsync(
        string userId,
        int take = 25,
        CancellationToken cancellationToken = default);

    Task<bool> MarkAsReadAsync(
        int notificationId,
        string userId,
        CancellationToken cancellationToken = default);

    Task<int> MarkAllAsReadAsync(
        string userId,
        CancellationToken cancellationToken = default);
}
