using Bartrix.Modules.Auth.Contracts;

namespace Bartrix.Modules.Auth.Application;

public interface IAuthService
{
    Task<AuthTokensResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<PhoneOtpChallengeResponse> RequestPhoneOtpAsync(RequestPhoneOtpRequest request, CancellationToken cancellationToken);

    Task<AuthTokensResponse> VerifyPhoneOtpAsync(VerifyPhoneOtpRequest request, CancellationToken cancellationToken);

    Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task<UserProfileResponse?> GetMeAsync(Guid userId, CancellationToken cancellationToken);
}
