using Bartrix.Modules.Messaging.Application;
using Bartrix.Modules.Messaging.Contracts;

namespace Bartrix.Modules.Messaging.Infrastructure;

public sealed class NoOpConversationRealtimeNotifier : IConversationRealtimeNotifier
{
    public Task NotifyMessageSentAsync(Guid conversationId, ConversationMessageResponse message, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    public Task NotifyConversationUpdatedAsync(Guid userId, ConversationListItemResponse conversation, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
