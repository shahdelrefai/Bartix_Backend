using Bartrix.Modules.Messaging.Contracts;

namespace Bartrix.Modules.Messaging.Application;

public interface IConversationRealtimeNotifier
{
    Task NotifyMessageSentAsync(Guid conversationId, ConversationMessageResponse message, CancellationToken cancellationToken);

    /// <summary>Pushes a conversation-list update (e.g. new last message / unread count) to a specific user.</summary>
    Task NotifyConversationUpdatedAsync(Guid userId, ConversationListItemResponse conversation, CancellationToken cancellationToken);
}
