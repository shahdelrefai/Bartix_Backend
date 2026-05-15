namespace Bartrix.Modules.Reputation.Contracts;

public sealed record ReviewResponse(
    Guid Id,
    Guid TradeProposalId,
    Guid ReviewerUserId,
    Guid RevieweeUserId,
    int Rating,
    string? Comment,
    DateTimeOffset CreatedAtUtc);
