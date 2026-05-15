namespace Bartrix.BuildingBlocks.Authentication;

public sealed record OtpChallengeRequest(
    string PhoneNumber,
    string Purpose);
