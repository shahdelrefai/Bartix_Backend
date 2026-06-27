namespace Bartrix.Modules.Auth.Contracts;

public sealed record UpdateProfileRequest(
    string? Name,
    string? ProfileImageUrl,
    bool? Is2faEnabled);
