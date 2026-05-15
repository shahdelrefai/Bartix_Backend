namespace Bartrix.Modules.Auth.Domain;

public sealed class RefreshTokenSession
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public Guid UserAccountId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public DateTimeOffset? RevokedAtUtc { get; private set; }

    public UserAccount UserAccount { get; private set; } = null!;

    private RefreshTokenSession()
    {
        TokenHash = string.Empty;
    }

    public RefreshTokenSession(
        Guid userAccountId,
        string tokenHash,
        DateTimeOffset createdAtUtc,
        DateTimeOffset expiresAtUtc)
    {
        UserAccountId = userAccountId;
        TokenHash = tokenHash;
        CreatedAtUtc = createdAtUtc;
        ExpiresAtUtc = expiresAtUtc;
    }

    public bool IsActive(DateTimeOffset nowUtc)
    {
        return RevokedAtUtc is null && ExpiresAtUtc > nowUtc;
    }

    public void Revoke(DateTimeOffset revokedAtUtc)
    {
        RevokedAtUtc = revokedAtUtc;
    }
}
