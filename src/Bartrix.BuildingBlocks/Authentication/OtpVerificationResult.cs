namespace Bartrix.BuildingBlocks.Authentication;

public sealed record OtpVerificationResult(
    bool IsVerified,
    string? FailureReason = null);
