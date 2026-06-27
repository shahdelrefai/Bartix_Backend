using Bartrix.Modules.Delivery.Contracts;

namespace Bartrix.Modules.Delivery.Application;

public interface IDeliveryService
{
    Task<DeliveryResponse> GetTradeDeliveryAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken);

    Task<DeliveryResponse> UpdateTradeDeliveryAsync(Guid userId, Guid tradeProposalId, UpdateDeliveryRequest request, CancellationToken cancellationToken);

    Task<DeliveryResponse> MarkDeliveredAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken);

    Task<DeliveryResponse> ConfirmAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken);

    Task<DeliveryResponse> CancelAsync(Guid userId, Guid tradeProposalId, CancellationToken cancellationToken);

    Task<IReadOnlyList<DeliveryResponse>> GetAllDeliveriesAsync(CancellationToken cancellationToken);
}
