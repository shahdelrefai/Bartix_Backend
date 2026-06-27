namespace Bartrix.Modules.Auth.Domain;

/// <summary>
/// An email pre-approved to receive the <c>admin</c> role on registration.
/// Mirrors the Firebase <c>AdminWhitelist</c> collection.
/// </summary>
public sealed class AdminWhitelistEntry
{
    public string NormalizedEmail { get; private set; } = string.Empty;

    public Guid? AddedBy { get; private set; }

    public DateTimeOffset AddedAtUtc { get; private set; }

    private AdminWhitelistEntry()
    {
    }

    public AdminWhitelistEntry(string normalizedEmail, Guid? addedBy, DateTimeOffset addedAtUtc)
    {
        NormalizedEmail = normalizedEmail;
        AddedBy = addedBy;
        AddedAtUtc = addedAtUtc;
    }
}
