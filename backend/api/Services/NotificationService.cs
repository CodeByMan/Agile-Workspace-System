using api.Data;
using api.Dtos.Notifications;
using api.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace api.Services;

public sealed class NotificationService(ApplicationDbContext context) : INotificationService
{
    public async Task<NotificationSummaryDto> GetForUserAsync(
        string userId,
        int take = 25,
        CancellationToken cancellationToken = default)
    {
        take = Math.Clamp(take <= 0 ? 25 : take, 1, 100);
        var query = context.Notifications
            .AsNoTracking()
            .Where(x => x.UserId == userId);

        var total = await query.CountAsync(cancellationToken);
        var unread = await query.CountAsync(x => !x.IsRead, cancellationToken);
        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .Select(x => new NotificationDto
            {
                Id = x.Id,
                Type = x.Type,
                Title = x.Title,
                Message = x.Message,
                Link = x.Link,
                TaskItemId = x.TaskItemId,
                IsRead = x.IsRead,
                CreatedAt = x.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return new NotificationSummaryDto
        {
            Total = total,
            Unread = unread,
            Items = items
        };
    }

    public async Task<bool> MarkAsReadAsync(
        int notificationId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var updated = await context.Notifications
            .Where(x => x.Id == notificationId && x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.IsRead, true)
                    .SetProperty(x => x.ReadAt, DateTime.UtcNow),
                cancellationToken);

        if (updated > 0)
        {
            return true;
        }

        return await context.Notifications
            .AsNoTracking()
            .AnyAsync(x => x.Id == notificationId && x.UserId == userId, cancellationToken);
    }

    public Task<int> MarkAllAsReadAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        context.Notifications
            .Where(x => x.UserId == userId && !x.IsRead)
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(x => x.IsRead, true)
                    .SetProperty(x => x.ReadAt, DateTime.UtcNow),
                cancellationToken);
}
