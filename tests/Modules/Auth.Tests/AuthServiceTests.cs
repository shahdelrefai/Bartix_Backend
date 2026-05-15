using Bartrix.BuildingBlocks.Authentication;
using Bartrix.Modules.Auth.Application;
using Bartrix.Modules.Auth.Contracts;
using Bartrix.Modules.Auth.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bartrix.Modules.Auth.Tests;

public sealed class AuthServiceTests
{
    [Fact]
    public async Task RegisterAsync_CreatesUser_AndReturnsTokens()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var response = await service.RegisterAsync(
            new RegisterRequest("user@example.com", "Pass123!", "Test User", "+201001112223"),
            CancellationToken.None);

        Assert.NotEqual(Guid.Empty, response.User.UserId);
        Assert.Equal("user@example.com", response.User.Email);
        Assert.False(string.IsNullOrWhiteSpace(response.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(response.RefreshToken));
        Assert.True(await dbContext.Users.AnyAsync(x => x.NormalizedEmail == "USER@EXAMPLE.COM"));
    }

    [Fact]
    public async Task PhoneOtpFlow_VerifiesPhone_AndReturnsTokens()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        await service.RegisterAsync(
            new RegisterRequest("phone@example.com", "Pass123!", "Phone User", "+201234567890"),
            CancellationToken.None);

        var challenge = await service.RequestPhoneOtpAsync(
            new RequestPhoneOtpRequest("+201234567890"),
            CancellationToken.None);

        Assert.Equal("123456", challenge.DebugCode);

        var response = await service.VerifyPhoneOtpAsync(
            new VerifyPhoneOtpRequest(challenge.ChallengeId, "+201234567890", "123456"),
            CancellationToken.None);

        Assert.True(response.User.IsPhoneVerified);
        Assert.Equal("+201234567890", response.User.PhoneNumber);
    }

    [Fact]
    public async Task RefreshAsync_RotatesRefreshToken()
    {
        await using var dbContext = CreateDbContext();
        var service = CreateService(dbContext);

        var registerResponse = await service.RegisterAsync(
            new RegisterRequest("refresh@example.com", "Pass123!", "Refresh User", null),
            CancellationToken.None);

        var refreshed = await service.RefreshAsync(
            new RefreshTokenRequest(registerResponse.RefreshToken),
            CancellationToken.None);

        Assert.NotEqual(registerResponse.RefreshToken, refreshed.RefreshToken);
        Assert.True(await dbContext.RefreshTokenSessions.CountAsync() >= 2);
    }

    [Fact]
    public void PasswordHasher_VerifiesExpectedPassword()
    {
        var hasher = new Pbkdf2PasswordHasher();
        var hashed = hasher.Hash("Secret123!");

        Assert.True(hasher.Verify(hashed, "Secret123!"));
        Assert.False(hasher.Verify(hashed, "WrongSecret123!"));
    }

    private static AuthDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AuthDbContext(options);
    }

    private static AuthService CreateService(AuthDbContext dbContext)
    {
        var jwtOptions = Options.Create(new JwtOptions
        {
            Issuer = "Bartrix.Tests",
            Audience = "Bartrix.Tests.Client",
            SigningKey = "bartrix-tests-signing-key-1234567890",
            AccessTokenMinutes = 15,
            ClockSkewSeconds = 0
        });

        var refreshOptions = Options.Create(new RefreshTokenOptions
        {
            ExpiryDays = 30,
            TokenLengthBytes = 64
        });

        var otpOptions = new OtpOptions
        {
            Provider = "LocalMock",
            CodeLength = 6,
            ExpiryMinutes = 10,
            MaxAttempts = 5,
            DevelopmentCode = "123456"
        };

        return new AuthService(
            dbContext,
            new Pbkdf2PasswordHasher(),
            new JwtTokenService(jwtOptions),
            new RefreshTokenService(refreshOptions),
            new LocalMockOtpProvider(otpOptions, new FixedTimeProvider()),
            Options.Create(otpOptions),
            new FixedTimeProvider());
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private static readonly DateTimeOffset FixedUtcNow = new(2026, 5, 7, 20, 0, 0, TimeSpan.Zero);

        public override DateTimeOffset GetUtcNow() => FixedUtcNow;
    }
}
