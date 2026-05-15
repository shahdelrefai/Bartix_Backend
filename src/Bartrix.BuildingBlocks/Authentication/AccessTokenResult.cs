namespace Bartrix.BuildingBlocks.Authentication;

public sealed record AccessTokenResult(
    string Token,
    DateTimeOffset ExpiresAtUtc);
