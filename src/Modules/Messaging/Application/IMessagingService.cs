using Bartrix.Modules.Messaging.Contracts;

namespace Bartrix.Modules.Messaging.Application;

public interface IMessagingService
{
    Task<IReadOnlyList<ConversationListItemResponse>> GetMyConversationsAsync(Guid userId, CancellationToken cancellationToken);

    Task<ConversationResponse> GetTradeConversationAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken);

    Task<ConversationMessageResponse> SendTradeMessageAsync(Guid userId, Guid tradeProposalId, SendMessageRequest request, CancellationToken cancellationToken);
}
