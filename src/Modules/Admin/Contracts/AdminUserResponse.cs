namespace Bartrix.Modules.Admin.Contracts;

public sealed record AdminUserResponse(
    Guid Id,
    string Name,
    string Email,
    string Role,
    string LanguageCode,
    string? ProfileImageUrl,
    bool IsSuspended,
    bool IsPremiumActive,
    decimal WalletBalance,
    DateTimeOffset CreatedAt);
