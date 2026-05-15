namespace Bartrix.BuildingBlocks.Authentication;

public sealed record RefreshTokenResult(
    string Token,
    DateTimeOffset ExpiresAtUtc);
