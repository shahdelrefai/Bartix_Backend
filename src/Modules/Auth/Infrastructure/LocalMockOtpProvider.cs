using Bartrix.BuildingBlocks.Authentication;

namespace Bartrix.Modules.Auth.Infrastructure;

public sealed class LocalMockOtpProvider : IOtpProvider
{
    private readonly OtpOptions _options;
    private readonly TimeProvider _timeProvider;

    public LocalMockOtpProvider(OtpOptions options, TimeProvider timeProvider)
    {
        _options = options;
        _timeProvider = timeProvider;
    }

    public Task<OtpChallengeResult> SendChallengeAsync(
        OtpChallengeRequest request,
        CancellationToken cancellationToken = default)
    {
        var expiresAtUtc = _timeProvider.GetUtcNow().AddMinutes(_options.ExpiryMinutes);
        return Task.FromResult(new OtpChallengeResult(null, expiresAtUtc));
    }

    public Task<OtpVerificationResult> VerifyChallengeAsync(
        OtpVerificationRequest request,
        CancellationToken cancellationToken = default)
    {
        var isVerified = string.Equals(request.Code, _options.DevelopmentCode, StringComparison.Ordinal);
        return Task.FromResult(
            isVerified
                ? new OtpVerificationResult(true)
                : new OtpVerificationResult(false, "Verification code is invalid."));
    }
}
