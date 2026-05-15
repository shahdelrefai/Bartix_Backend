namespace Bartrix.Modules.Profiles.Contracts;

public sealed record UpdateMyProfileRequest(
    string DisplayName,
    string? Bio,
    string? Location,
    string? AvatarUrl);
