using System.Security.Cryptography;
using Bartrix.BuildingBlocks.Authentication;
using Microsoft.Extensions.Options;

namespace Bartrix.Modules.Auth.Infrastructure;

public sealed class RefreshTokenService : IRefreshTokenService
{
    private readonly RefreshTokenOptions _options;

    public RefreshTokenService(IOptions<RefreshTokenOptions> options)
    {
        _options = options.Value;
    }

    public RefreshTokenResult CreateRefreshToken(DateTimeOffset issuedAtUtc)
    {
        var bytes = RandomNumberGenerator.GetBytes(_options.TokenLengthBytes);
        var token = Convert.ToBase64String(bytes);
        var expiresAtUtc = issuedAtUtc.AddDays(_options.ExpiryDays);

        return new RefreshTokenResult(token, expiresAtUtc);
    }
}
