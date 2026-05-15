namespace Bartrix.Modules.Reputation.Application;

public sealed record TradeParticipantSnapshot(
    Guid TradeProposalId,
    Guid SenderUserId,
    Guid ReceiverUserId,
    string Status);
