using Bartrix.Modules.Auth.Contracts;

namespace Bartrix.Modules.Auth.Application;

public interface IAuthService
{
    Task<AuthTokensResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<AuthTokensResponse> GoogleSignInAsync(GoogleSignInRequest request, CancellationToken cancellationToken);

    Task<PhoneOtpChallengeResponse> RequestPhoneOtpAsync(RequestPhoneOtpRequest request, CancellationToken cancellationToken);

    Task<AuthTokensResponse> VerifyPhoneOtpAsync(VerifyPhoneOtpRequest request, CancellationToken cancellationToken);

    Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task<UserProfileResponse?> GetMeAsync(Guid userId, CancellationToken cancellationToken);

    Task<UserProfileResponse> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);

    Task UpdateLanguageAsync(Guid userId, UpdateLanguageRequest request, CancellationToken cancellationToken);

    Task BlockUserAsync(Guid userId, Guid blockedUserId, CancellationToken cancellationToken);

    Task UnblockUserAsync(Guid userId, Guid blockedUserId, CancellationToken cancellationToken);

    Task<PublicUserResponse?> GetPublicUserAsync(Guid userId, CancellationToken cancellationToken);

    // Email OTP / 2FA
    Task RequestEmailOtpAsync(string email, string purpose, CancellationToken cancellationToken);
    Task<AuthTokensResponse> VerifyEmailOtpAsync(string email, string code, CancellationToken cancellationToken);

    // Password reset
    Task RequestPasswordResetAsync(string email, CancellationToken cancellationToken);
    Task ConfirmPasswordResetAsync(string email, string code, string newPassword, CancellationToken cancellationToken);

    // Logout
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken);

    // Premium
    Task ActivatePremiumAsync(Guid userId, string paymobPaymentId, CancellationToken cancellationToken);
    Task<(bool IsActive, DateTimeOffset? ExpiresAt)> GetPremiumStatusAsync(Guid userId, CancellationToken cancellationToken);
}
