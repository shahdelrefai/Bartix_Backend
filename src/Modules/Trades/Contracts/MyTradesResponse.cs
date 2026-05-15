namespace Bartrix.Modules.Trades.Contracts;

public sealed record MyTradesResponse(
    IReadOnlyList<TradeSummaryResponse> Sent,
    IReadOnlyList<TradeSummaryResponse> Received);
