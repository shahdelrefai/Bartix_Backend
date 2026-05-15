namespace Bartrix.Modules.Delivery.Application;

public interface ITradeDeliveryReader
{
    Task<TradeDeliveryAccessSnapshot?> GetTradeAsync(Guid tradeProposalId, CancellationToken cancellationToken);
}
