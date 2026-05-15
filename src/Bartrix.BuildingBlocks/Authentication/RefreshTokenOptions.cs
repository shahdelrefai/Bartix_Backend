namespace Bartrix.BuildingBlocks.Authentication;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "Authentication:RefreshTokens";

    public int ExpiryDays { get; init; } = 30;

    public int TokenLengthBytes { get; init; } = 64;
}
