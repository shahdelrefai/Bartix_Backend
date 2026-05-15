namespace Bartrix.BuildingBlocks.Authentication;

public sealed class OtpOptions
{
    public const string SectionName = "Authentication:Otp";

    public string Provider { get; init; } = "LocalMock";

    public int CodeLength { get; init; } = 6;

    public int ExpiryMinutes { get; init; } = 10;

    public int MaxAttempts { get; init; } = 5;

    public string DevelopmentCode { get; init; } = "123456";
}
