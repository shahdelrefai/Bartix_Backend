using System.Security.Claims;

namespace Bartrix.BuildingBlocks.Authentication;

public interface IJwtTokenService
{
    AccessTokenResult CreateAccessToken(
        string subject,
        IEnumerable<Claim> claims,
        DateTimeOffset issuedAtUtc);
}
