using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Bartrix.Api.Hubs;

[Authorize]
public sealed class TradeChatHub : Hub
{
    public Task JoinConversation(Guid conversationId)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, ToConversationGroup(conversationId));
    }

    public Task LeaveConversation(Guid conversationId)
    {
        return Groups.RemoveFromGroupAsync(Context.ConnectionId, ToConversationGroup(conversationId));
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!string.IsNullOrWhiteSpace(userId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, ToUserGroup(userId));
        }

        await base.OnConnectedAsync();
    }

    public static string ToConversationGroup(Guid conversationId) => $"conversation:{conversationId:N}";

    public static string ToUserGroup(string userId) => $"user:{userId}";
}
