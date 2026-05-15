namespace Bartrix.BuildingBlocks.Authentication;

public sealed record OtpVerificationRequest(
    string PhoneNumber,
    string Code,
    string? ProviderChallengeId);
