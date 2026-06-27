namespace Bartrix.Modules.Auth.Contracts;

/// <summary>
/// Full user payload. JSON property names serialize (camelCase) to exactly the keys
/// the Flutter <c>UserModel.fromJson</c> expects: id, name, email, role, languageCode,
/// profileImageUrl, is2faEnabled, isPremiumActive, isSuspended, premiumExpiresAt,
/// walletBalance, blockedUserIds, favouriteProductIds.
/// </summary>
public sealed record UserProfileResponse(
    Guid Id,
    string Email,
    string Name,
    string? PhoneNumber,
    bool IsPhoneVerified,
    string Role,
    string LanguageCode,
    string? ProfileImageUrl,
    bool Is2faEnabled,
    bool IsPremiumActive,
    bool IsSuspended,
    DateTimeOffset? PremiumExpiresAt,
    decimal WalletBalance,
    IReadOnlyList<Guid> BlockedUserIds,
    IReadOnlyList<Guid> FavouriteProductIds,
    bool IsAnonymous = false);
