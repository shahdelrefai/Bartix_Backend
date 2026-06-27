namespace Bartrix.Modules.Auth.Domain;

public sealed class UserAccount
{
    public Guid Id { get; private set; } = Guid.NewGuid();

    public string Email { get; private set; }

    public string NormalizedEmail { get; private set; }

    public string PasswordHash { get; private set; }

    public string DisplayName { get; private set; }

    public string? PhoneNumber { get; private set; }

    public string? NormalizedPhoneNumber { get; private set; }

    public bool IsPhoneVerified { get; private set; }

    // ─── Parity fields (mirrors the Firebase Users document) ──────────────
    public string Role { get; private set; } = UserRoles.User;

    public bool IsSuspended { get; private set; }

    public bool IsPremiumActive { get; private set; }

    public DateTimeOffset? PremiumExpiresAtUtc { get; private set; }

    public bool Is2faEnabled { get; private set; }

    public string LanguageCode { get; private set; } = "en";

    public string? ProfileImageUrl { get; private set; }

    /// <summary>
    /// Denormalized wallet balance, mirroring the Firebase user document so the
    /// client can read it from <c>/api/auth/me</c>. The Wallet module is the only
    /// writer and always updates it inside the same transaction as a ledger entry.
    /// </summary>
    public decimal WalletBalance { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset? LastLoginAtUtc { get; private set; }

    private UserAccount()
    {
        Email = string.Empty;
        NormalizedEmail = string.Empty;
        PasswordHash = string.Empty;
        DisplayName = string.Empty;
    }

    public UserAccount(
        string email,
        string normalizedEmail,
        string passwordHash,
        string displayName,
        string? phoneNumber,
        string? normalizedPhoneNumber,
        DateTimeOffset createdAtUtc,
        string role = UserRoles.User)
    {
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        PhoneNumber = phoneNumber;
        NormalizedPhoneNumber = normalizedPhoneNumber;
        CreatedAtUtc = createdAtUtc;
        Role = role;
    }

    public void MarkPhoneVerified(string phoneNumber, string normalizedPhoneNumber)
    {
        PhoneNumber = phoneNumber;
        NormalizedPhoneNumber = normalizedPhoneNumber;
        IsPhoneVerified = true;
    }

    public void MarkLoggedIn(DateTimeOffset loggedInAtUtc)
    {
        LastLoginAtUtc = loggedInAtUtc;
    }

    public void UpdateProfile(string? displayName, string? profileImageUrl, bool? is2faEnabled)
    {
        if (!string.IsNullOrWhiteSpace(displayName))
        {
            DisplayName = displayName.Trim();
        }

        if (profileImageUrl is not null)
        {
            ProfileImageUrl = string.IsNullOrWhiteSpace(profileImageUrl) ? null : profileImageUrl.Trim();
        }

        if (is2faEnabled.HasValue)
        {
            Is2faEnabled = is2faEnabled.Value;
        }
    }

    public void SetLanguage(string languageCode)
    {
        if (!string.IsNullOrWhiteSpace(languageCode))
        {
            LanguageCode = languageCode.Trim();
        }
    }

    public void SetRole(string role)
    {
        if (!UserRoles.IsValid(role))
        {
            throw new InvalidOperationException($"'{role}' is not a valid role.");
        }

        Role = role;
    }

    public void SetSuspended(bool suspended) => IsSuspended = suspended;

    public void ApplyPremium(bool isActive, DateTimeOffset? expiresAtUtc)
    {
        IsPremiumActive = isActive;
        PremiumExpiresAtUtc = expiresAtUtc;
    }

    /// <summary>Used by the Wallet module to keep the denormalized balance in sync.</summary>
    public void SetWalletBalance(decimal balance) => WalletBalance = balance;

    public void SetPasswordHash(string newHash) => PasswordHash = newHash;
}
