using System.Collections.Concurrent;
using System.Security.Cryptography;
using Bartrix.BuildingBlocks.Authentication;
using Bartrix.Modules.Auth.Application;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bartrix.Modules.Auth.Infrastructure;

public sealed class InMemoryEmailOtpService : IEmailOtpService
{
    private sealed record OtpEntry(string CodeHash, DateTimeOffset ExpiresAtUtc, int AttemptCount);

    private readonly ConcurrentDictionary<string, OtpEntry> _store = new(StringComparer.OrdinalIgnoreCase);
    private readonly IPasswordHasher _passwordHasher;
    private readonly OtpOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<InMemoryEmailOtpService> _logger;

    public InMemoryEmailOtpService(
        IPasswordHasher passwordHasher,
        IOptions<OtpOptions> options,
        TimeProvider timeProvider,
        ILogger<InMemoryEmailOtpService> logger)
    {
        _passwordHasher = passwordHasher;
        _options = options.Value;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public Task SendOtpAsync(string email, string purpose, CancellationToken cancellationToken)
    {
        var code = GenerateCode();
        var key = MakeKey(email, purpose);
        var entry = new OtpEntry(
            _passwordHasher.Hash(code),
            _timeProvider.GetUtcNow().AddMinutes(_options.ExpiryMinutes),
            0);
        _store[key] = entry;
        _logger.LogInformation("[EMAIL OTP] To={Email} Purpose={Purpose} Code={Code}", email, purpose, code);
        return Task.CompletedTask;
    }

    public Task<bool> VerifyOtpAsync(string email, string purpose, string code, CancellationToken cancellationToken)
    {
        var key = MakeKey(email, purpose);
        if (!_store.TryGetValue(key, out var entry))
            return Task.FromResult(false);

        if (_timeProvider.GetUtcNow() > entry.ExpiresAtUtc || entry.AttemptCount >= _options.MaxAttempts)
        {
            _store.TryRemove(key, out _);
            return Task.FromResult(false);
        }

        if (!_passwordHasher.Verify(entry.CodeHash, code))
        {
            _store[key] = entry with { AttemptCount = entry.AttemptCount + 1 };
            return Task.FromResult(false);
        }

        _store.TryRemove(key, out _);
        return Task.FromResult(true);
    }

    private string GenerateCode()
    {
        if (string.Equals(_options.Provider, "LocalMock", StringComparison.OrdinalIgnoreCase)
            && !string.IsNullOrWhiteSpace(_options.DevelopmentCode))
            return _options.DevelopmentCode;

        var maxValue = (int)Math.Pow(10, _options.CodeLength);
        return RandomNumberGenerator.GetInt32(0, maxValue).ToString($"D{_options.CodeLength}");
    }

    private static string MakeKey(string email, string purpose) =>
        $"{email.Trim().ToUpperInvariant()}:{purpose}";
}
