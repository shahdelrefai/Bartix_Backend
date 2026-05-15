namespace Bartrix.Modules.Auth.Contracts;

public sealed record VerifyPhoneOtpRequest(
    Guid ChallengeId,
    string PhoneNumber,
    string Code);
