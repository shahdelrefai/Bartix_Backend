namespace Bartrix.Modules.Trades.Contracts;

public sealed record TradeHistoryResponse(
    Guid Id,
    Guid TradeProposalId,
    string Action,
    Guid PerformedByUserId,
    string? PerformedByUserName,
    string? Details,
    DateTimeOffset TimestampUtc);
