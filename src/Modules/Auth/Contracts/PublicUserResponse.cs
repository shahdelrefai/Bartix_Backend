namespace Bartrix.Modules.Auth.Contracts;

/// <summary>Public-facing subset of a user, returned when another user is viewed.</summary>
public sealed record PublicUserResponse(
    Guid Id,
    string Name,
    string? ProfileImageUrl,
    string Role,
    bool IsPremiumActive);
