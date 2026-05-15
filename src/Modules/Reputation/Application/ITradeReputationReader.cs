namespace Bartrix.Modules.Reputation.Application;

public interface ITradeReputationReader
{
    Task<TradeParticipantSnapshot?> GetTradeAsync(Guid tradeProposalId, CancellationToken cancellationToken);
}
