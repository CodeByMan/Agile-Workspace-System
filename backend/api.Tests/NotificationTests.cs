using api.Data;
using api.Models;
using api.Services;

namespace api.Tests;

public sealed class NotificationTests
{
    [Fact]
    public async Task TotalsCoverAllNotifications_WhileItemsRespectTake()
    {
        await using var context = TestDb.CreateInMemory();
        var userId = "notification-user";
        for (var i = 0; i < 7; i++)
            context.Notifications.Add(new Notification { UserId = userId, Type = "Test", Title = $"N{i}", Message = "Message", IsRead = i < 2, CreatedAt = DateTime.UtcNow.AddMinutes(-i) });
        await context.SaveChangesAsync();

        var service = new NotificationService(context);
        var result = await service.GetForUserAsync(userId, take: 3);

        Assert.Equal(7, result.Total);
        Assert.Equal(5, result.Unread);
        Assert.Equal(3, result.Items.Count);
    }
}
