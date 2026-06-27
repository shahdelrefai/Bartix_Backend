namespace Bartrix.Modules.Reputation.Contracts;

/// <summary>Direct user-to-user review (mirrors the Firebase <c>reviews</c> collection).</summary>
public sealed record CreateUserReviewRequest(
    int Rating,
    string? Title,
    string? Content,
    string? ReviewerName = null);
