namespace Bartrix.BuildingBlocks.Authentication;

public sealed record OtpChallengeResult(
    string? ProviderChallengeId,
    DateTimeOffset ExpiresAtUtc);
