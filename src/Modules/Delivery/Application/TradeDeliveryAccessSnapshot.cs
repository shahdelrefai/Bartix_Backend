namespace Bartrix.Modules.Delivery.Application;

public sealed record TradeDeliveryAccessSnapshot(
    Guid TradeProposalId,
    Guid SenderUserId,
    Guid ReceiverUserId,
    string Status);
