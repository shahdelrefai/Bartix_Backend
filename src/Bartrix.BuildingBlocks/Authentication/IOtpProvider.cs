namespace Bartrix.BuildingBlocks.Authentication;

public interface IOtpProvider
{
    Task<OtpChallengeResult> SendChallengeAsync(
        OtpChallengeRequest request,
        CancellationToken cancellationToken = default);

    Task<OtpVerificationResult> VerifyChallengeAsync(
        OtpVerificationRequest request,
        CancellationToken cancellationToken = default);
}
