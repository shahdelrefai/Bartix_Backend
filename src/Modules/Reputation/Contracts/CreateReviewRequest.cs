namespace Bartrix.Modules.Reputation.Contracts;

public sealed record CreateReviewRequest(
    int Rating,
    string? Comment);
