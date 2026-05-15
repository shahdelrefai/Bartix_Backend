namespace Bartrix.Modules.Auth.Contracts;

public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName,
    string? PhoneNumber);
