namespace Bartrix.Modules.Profiles.Domain;

public sealed class UserProfile
{
    public Guid UserId { get; private set; }

    public string DisplayName { get; private set; }

    public string? Bio { get; private set; }

    public string? Location { get; private set; }

    public string? AvatarUrl { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    private UserProfile()
    {
        DisplayName = string.Empty;
    }

    public UserProfile(
        Guid userId,
        string displayName,
        string? bio,
        string? location,
        string? avatarUrl,
        DateTimeOffset createdAtUtc,
        DateTimeOffset updatedAtUtc)
    {
        UserId = userId;
        DisplayName = displayName;
        Bio = bio;
        Location = location;
        AvatarUrl = avatarUrl;
        CreatedAtUtc = createdAtUtc;
        UpdatedAtUtc = updatedAtUtc;
    }

    public void Update(string displayName, string? bio, string? location, string? avatarUrl, DateTimeOffset updatedAtUtc)
    {
        DisplayName = displayName;
        Bio = bio;
        Location = location;
        AvatarUrl = avatarUrl;
        UpdatedAtUtc = updatedAtUtc;
    }
}
