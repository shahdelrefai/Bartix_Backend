namespace Bartrix.Modules.Messaging.Contracts;

public sealed record ConversationListItemResponse(
    Guid Id,
    Guid TradeProposalId,
    Guid OtherUserId,
    DateTimeOffset UpdatedAtUtc,
    ConversationMessageResponse? LastMessage);
