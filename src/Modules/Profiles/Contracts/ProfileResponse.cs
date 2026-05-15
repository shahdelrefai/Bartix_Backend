namespace Bartrix.Modules.Profiles.Contracts;

public sealed record ProfileResponse(
    Guid UserId,
    string DisplayName,
    string? Bio,
    string? Location,
    string? AvatarUrl,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset UpdatedAtUtc);
