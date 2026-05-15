namespace Bartrix.Modules.Auth.Domain;

public sealed class PhoneOtpChallenge
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string PhoneNumber { get; private set; }

    public string NormalizedPhoneNumber { get; private set; }

    public string Purpose { get; private set; }

    public string CodeHash { get; private set; }

    public string? ProviderChallengeId { get; private set; }

    public int MaxAttempts { get; private set; }

    public int AttemptCount { get; private set; }

    public bool IsConsumed { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    private PhoneOtpChallenge()
    {
        PhoneNumber = string.Empty;
        NormalizedPhoneNumber = string.Empty;
        Purpose = string.Empty;
        CodeHash = string.Empty;
    }

    public PhoneOtpChallenge(
        string phoneNumber,
        string normalizedPhoneNumber,
        string purpose,
        string codeHash,
        string? providerChallengeId,
        int maxAttempts,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        PhoneNumber = phoneNumber;
        NormalizedPhoneNumber = normalizedPhoneNumber;
        Purpose = purpose;
        CodeHash = codeHash;
        ProviderChallengeId = providerChallengeId;
        MaxAttempts = maxAttempts;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsAvailable(DateTimeOffset nowUtc)
    {
        return !IsConsumed && AttemptCount < MaxAttempts && ExpiresAtUtc > nowUtc;
    }

    public void RegisterFailedAttempt()
    {
        AttemptCount++;
    }

    public void Consume()
    {
        IsConsumed = true;
    }
}
