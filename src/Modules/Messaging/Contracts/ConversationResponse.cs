namespace Bartrix.Modules.Messaging.Contracts;

public sealed record ConversationResponse(
    Guid Id,
    Guid? TradeProposalId,
    Guid ParticipantAUserId,
    Guid ParticipantBUserId,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    IReadOnlyList<ConversationMessageResponse> Messages,
    int UnreadCount = 0);
