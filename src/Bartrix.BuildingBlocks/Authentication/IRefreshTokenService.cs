namespace Bartrix.BuildingBlocks.Authentication;

public interface IRefreshTokenService
{
    RefreshTokenResult CreateRefreshToken(DateTimeOffset issuedAtUtc);
}
