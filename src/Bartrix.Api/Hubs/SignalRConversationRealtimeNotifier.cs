using Bartrix.Modules.Messaging.Application;
using Bartrix.Modules.Messaging.Contracts;
using Microsoft.AspNetCore.SignalR;

namespace Bartrix.Api.Hubs;

public sealed class SignalRConversationRealtimeNotifier : IConversationRealtimeNotifier
{
    private readonly IHubContext<TradeChatHub> _hubContext;

    public SignalRConversationRealtimeNotifier(IHubContext<TradeChatHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public Task NotifyMessageSentAsync(Guid conversationId, ConversationMessageResponse message, CancellationToken cancellationToken)
    {
        return _hubContext.Clients
            .Group(TradeChatHub.ToConversationGroup(conversationId))
            .SendAsync("messageReceived", message, cancellationToken);
    }

    public Task NotifyConversationUpdatedAsync(Guid userId, ConversationListItemResponse conversation, CancellationToken cancellationToken)
    {
        return _hubContext.Clients
            .Group(TradeChatHub.ToUserGroup(userId.ToString()))
            .SendAsync("conversationUpdated", conversation, cancellationToken);
    }
}
