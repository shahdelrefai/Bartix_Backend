namespace Bartrix.Modules.Auth.Contracts;

public sealed record UserProfileResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string? PhoneNumber,
    bool IsPhoneVerified);
