namespace Bartrix.Modules.Messaging.Application;

public interface ITradeMessagingReader
{
    Task<TradeConversationAccessSnapshot?> GetTradeAsync(Guid tradeProposalId, CancellationToken cancellationToken);
}
