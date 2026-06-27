namespace Bartrix.Modules.Admin.Contracts;

public sealed record AdminStatsResponse(
    int TotalUsers,
    int TotalProducts,
    int TotalTrades,
    int ActiveTrades,
    int CompletedTrades,
    int PendingTrades,
    IReadOnlyDictionary<string, int> UsersByRole,
    int TotalReports,
    int PendingReports,
    DateTimeOffset LastUpdated);
