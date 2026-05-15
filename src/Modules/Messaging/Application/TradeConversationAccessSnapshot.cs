namespace Bartrix.Modules.Messaging.Application;

public sealed record TradeConversationAccessSnapshot(
    Guid TradeProposalId,
    Guid SenderUserId,
    Guid ReceiverUserId);
