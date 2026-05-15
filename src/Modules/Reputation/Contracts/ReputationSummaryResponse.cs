namespace Bartrix.Modules.Reputation.Contracts;

public sealed record ReputationSummaryResponse(
    Guid UserId,
    int TotalReviews,
    decimal AverageRating,
    IReadOnlyList<ReviewResponse> RecentReviews);
