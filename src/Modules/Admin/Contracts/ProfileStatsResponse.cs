namespace Bartrix.Modules.Admin.Contracts;

public sealed record ProfileStatsResponse(
    int ProductsCount,
    int CompletedTradesCount,
    int ReviewsCount);
