using Bartrix.Modules.Messaging.Contracts;

namespace Bartrix.Modules.Messaging.Application;

public interface IMessagingService
{
    Task<IReadOnlyList<ConversationListItemResponse>> GetMyConversationsAsync(Guid userId, CancellationToken cancellationToken);

    Task<ConversationResponse> GetTradeConversationAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken);

    Task<ConversationMessageResponse> SendTradeMessageAsync(Guid userId, Guid tradeProposalId, SendMessageRequest request, CancellationToken cancellationToken);

    // ─── Direct (non-trade) conversations ───────────────────────────────
    Task<ConversationResponse> GetOrCreateDirectConversationAsync(Guid userId, Guid otherUserId, CancellationToken cancellationToken);

    Task<ConversationResponse> GetConversationAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken);

    Task<ConversationMessageResponse> SendConversationMessageAsync(Guid userId, Guid conversationId, SendMessageRequest request, CancellationToken cancellationToken);

    Task MarkConversationReadAsync(Guid userId, Guid conversationId, CancellationToken cancellationToken);
}
