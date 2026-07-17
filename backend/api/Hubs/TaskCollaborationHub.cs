using System.Security.Claims;
using api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace api.Hubs;

[Authorize]
public sealed class TaskCollaborationHub : Hub
{
    private readonly ResourceAuthorizationService _authorization;
    public TaskCollaborationHub(ResourceAuthorizationService authorization) => _authorization = authorization;

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? Context.User?.FindFirstValue("nameid");
        if (!string.IsNullOrWhiteSpace(userId)) await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userId}");
        await base.OnConnectedAsync();
    }

    public async Task JoinProjectChannel(int projectId)
    {
        if (Context.User is null || !await _authorization.CanJoinProjectRealtimeAsync(Context.User, projectId, Context.ConnectionAborted))
            throw new HubException("You are not authorized to join this project channel.");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"project:{projectId}");
    }
    public Task LeaveProjectChannel(int projectId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, $"project:{projectId}");
}
