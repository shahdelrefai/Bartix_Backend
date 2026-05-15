using Bartrix.Modules.Messaging.Contracts;

namespace Bartrix.Modules.Messaging.Application;

public interface IConversationRealtimeNotifier
{
    Task NotifyMessageSentAsync(Guid conversationId, ConversationMessageResponse message, CancellationToken cancellationToken);
}
