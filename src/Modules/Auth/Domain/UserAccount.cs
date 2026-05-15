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
        DateTimeOffset createdAtUtc)
    {
        Email = email;
        NormalizedEmail = normalizedEmail;
        PasswordHash = passwordHash;
        DisplayName = displayName;
        PhoneNumber = phoneNumber;
        NormalizedPhoneNumber = normalizedPhoneNumber;
        CreatedAtUtc = createdAtUtc;
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
}
