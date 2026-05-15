using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Bartrix.BuildingBlocks.Authentication;
using Bartrix.Modules.Auth.Contracts;
using Bartrix.Modules.Auth.Domain;
using Bartrix.Modules.Auth.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Bartrix.Modules.Auth.Application;

public sealed class AuthService : IAuthService
{
    private const string PhoneSignInPurpose = "phone-sign-in";

    private readonly AuthDbContext _dbContext;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IOtpProvider _otpProvider;
    private readonly OtpOptions _otpOptions;
    private readonly TimeProvider _timeProvider;

    public AuthService(
        AuthDbContext dbContext,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IOtpProvider otpProvider,
        IOptions<OtpOptions> otpOptions,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _otpProvider = otpProvider;
        _otpOptions = otpOptions.Value;
        _timeProvider = timeProvider;
    }

    public async Task<AuthTokensResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
        var nowUtc = GetUtcNow();

        if (await _dbContext.Users.AnyAsync(x => x.NormalizedEmail == normalizedEmail, cancellationToken))
        {
            throw new AuthValidationException("An account with this email already exists.");
        }

        if (normalizedPhone is not null &&
            await _dbContext.Users.AnyAsync(x => x.NormalizedPhoneNumber == normalizedPhone, cancellationToken))
        {
            throw new AuthValidationException("An account with this phone number already exists.");
        }

        var user = new UserAccount(
            request.Email.Trim(),
            normalizedEmail,
            _passwordHasher.Hash(request.Password),
            request.DisplayName.Trim(),
            request.PhoneNumber?.Trim(),
            normalizedPhone,
            nowUtc);

        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, nowUtc, cancellationToken);
    }

    public async Task<AuthTokensResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var normalizedEmail = NormalizeEmail(request.Email);
        var user = await _dbContext.Users.SingleOrDefaultAsync(
            x => x.NormalizedEmail == normalizedEmail,
            cancellationToken);

        if (user is null || !_passwordHasher.Verify(user.PasswordHash, request.Password))
        {
            throw new AuthValidationException("Invalid email or password.");
        }

        var nowUtc = GetUtcNow();
        user.MarkLoggedIn(nowUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, nowUtc, cancellationToken);
    }

    public async Task<PhoneOtpChallengeResponse> RequestPhoneOtpAsync(RequestPhoneOtpRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber)
            ?? throw new AuthValidationException("Phone number is required.");

        var user = await _dbContext.Users.SingleOrDefaultAsync(
            x => x.NormalizedPhoneNumber == normalizedPhone,
            cancellationToken);

        if (user is null)
        {
            throw new AuthValidationException("No account is linked to this phone number.");
        }

        var nowUtc = GetUtcNow();
        var code = GenerateOtpCode();
        var providerResult = await _otpProvider.SendChallengeAsync(
            new OtpChallengeRequest(request.PhoneNumber.Trim(), PhoneSignInPurpose),
            cancellationToken);

        var challenge = new PhoneOtpChallenge(
            request.PhoneNumber.Trim(),
            normalizedPhone,
            PhoneSignInPurpose,
            _passwordHasher.Hash(code),
            providerResult.ProviderChallengeId,
            _otpOptions.MaxAttempts,
            nowUtc,
            providerResult.ExpiresAtUtc);

        _dbContext.PhoneOtpChallenges.Add(challenge);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new PhoneOtpChallengeResponse(
            challenge.Id,
            challenge.ExpiresAtUtc,
            string.Equals(_otpOptions.Provider, "LocalMock", StringComparison.OrdinalIgnoreCase) ? code : null);
    }

    public async Task<AuthTokensResponse> VerifyPhoneOtpAsync(VerifyPhoneOtpRequest request, CancellationToken cancellationToken)
    {
        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber)
            ?? throw new AuthValidationException("Phone number is required.");

        var challenge = await _dbContext.PhoneOtpChallenges.SingleOrDefaultAsync(
            x => x.Id == request.ChallengeId && x.NormalizedPhoneNumber == normalizedPhone,
            cancellationToken);

        if (challenge is null)
        {
            throw new AuthValidationException("OTP challenge was not found.");
        }

        var nowUtc = GetUtcNow();

        if (!challenge.IsAvailable(nowUtc))
        {
            throw new AuthValidationException("OTP challenge is no longer valid.");
        }

        if (!_passwordHasher.Verify(challenge.CodeHash, request.Code))
        {
            challenge.RegisterFailedAttempt();
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new AuthValidationException("Invalid verification code.");
        }

        var providerResult = await _otpProvider.VerifyChallengeAsync(
            new OtpVerificationRequest(request.PhoneNumber.Trim(), request.Code, challenge.ProviderChallengeId),
            cancellationToken);

        if (!providerResult.IsVerified)
        {
            challenge.RegisterFailedAttempt();
            await _dbContext.SaveChangesAsync(cancellationToken);
            throw new AuthValidationException(providerResult.FailureReason ?? "Verification failed.");
        }

        var user = await _dbContext.Users.SingleOrDefaultAsync(
            x => x.NormalizedPhoneNumber == normalizedPhone,
            cancellationToken);

        if (user is null)
        {
            throw new AuthValidationException("No account is linked to this phone number.");
        }

        challenge.Consume();
        user.MarkPhoneVerified(request.PhoneNumber.Trim(), normalizedPhone);
        user.MarkLoggedIn(nowUtc);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(user, nowUtc, cancellationToken);
    }

    public async Task<AuthTokensResponse> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var nowUtc = GetUtcNow();
        var refreshTokenHash = ComputeSha256(request.RefreshToken);

        var session = await _dbContext.RefreshTokenSessions
            .Include(x => x.UserAccount)
            .SingleOrDefaultAsync(x => x.TokenHash == refreshTokenHash, cancellationToken);

        if (session is null || !session.IsActive(nowUtc))
        {
            throw new AuthValidationException("Refresh token is invalid or expired.");
        }

        session.Revoke(nowUtc);
        session.UserAccount.MarkLoggedIn(nowUtc);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await IssueTokensAsync(session.UserAccount, nowUtc, cancellationToken);
    }

    public async Task<UserProfileResponse?> GetMeAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(x => x.Id == userId, cancellationToken);
        return user is null ? null : MapUser(user);
    }

    private async Task<AuthTokensResponse> IssueTokensAsync(
        UserAccount user,
        DateTimeOffset nowUtc,
        CancellationToken cancellationToken)
    {
        var claims = BuildClaims(user);
        var accessToken = _jwtTokenService.CreateAccessToken(user.Id.ToString(), claims, nowUtc);
        var refreshToken = _refreshTokenService.CreateRefreshToken(nowUtc);

        var session = new RefreshTokenSession(
            user.Id,
            ComputeSha256(refreshToken.Token),
            nowUtc,
            refreshToken.ExpiresAtUtc);

        _dbContext.RefreshTokenSessions.Add(session);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthTokensResponse(
            accessToken.Token,
            accessToken.ExpiresAtUtc,
            refreshToken.Token,
            refreshToken.ExpiresAtUtc,
            MapUser(user));
    }

    private IEnumerable<Claim> BuildClaims(UserAccount user)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Name, user.DisplayName),
            new(ClaimTypes.Email, user.Email)
        };

        if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            claims.Add(new(ClaimTypes.MobilePhone, user.PhoneNumber));
        }

        claims.Add(new("phone_verified", user.IsPhoneVerified.ToString().ToLowerInvariant()));

        return claims;
    }

    private static UserProfileResponse MapUser(UserAccount user)
    {
        return new UserProfileResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.PhoneNumber,
            user.IsPhoneVerified);
    }

    private DateTimeOffset GetUtcNow() => _timeProvider.GetUtcNow();

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToUpperInvariant();
    }

    private static string? NormalizePhoneNumber(string? phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return null;
        }

        var chars = phoneNumber.Where(ch => char.IsDigit(ch) || ch == '+').ToArray();
        return new string(chars);
    }

    private string GenerateOtpCode()
    {
        if (string.Equals(_otpOptions.Provider, "LocalMock", StringComparison.OrdinalIgnoreCase))
        {
            return _otpOptions.DevelopmentCode;
        }

        var maxValue = (int)Math.Pow(10, _otpOptions.CodeLength);
        var code = RandomNumberGenerator.GetInt32(0, maxValue);
        return code.ToString($"D{_otpOptions.CodeLength}");
    }

    private static string ComputeSha256(string value)
    {
        var bytes = Encoding.UTF8.GetBytes(value);
        return Convert.ToHexString(SHA256.HashData(bytes));
    }
}
