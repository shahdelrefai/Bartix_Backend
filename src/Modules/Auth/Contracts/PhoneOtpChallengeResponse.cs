namespace Bartrix.Modules.Auth.Contracts;

public sealed record PhoneOtpChallengeResponse(
    Guid ChallengeId,
    DateTimeOffset ExpiresAtUtc,
    string? DebugCode);
