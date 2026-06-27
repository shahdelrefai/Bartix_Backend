namespace Bartrix.Modules.Auth.Domain;

/// <summary>
/// A directional block: <see cref="UserId"/> has blocked <see cref="BlockedUserId"/>.
/// Mirrors the Firebase <c>blockedUserIds</c> array on the user document.
/// </summary>
public sealed class BlockedUser
{
    public Guid UserId { get; private set; }

    public Guid BlockedUserId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    private BlockedUser()
    {
    }

    public BlockedUser(Guid userId, Guid blockedUserId, DateTimeOffset createdAtUtc)
    {
        UserId = userId;
        BlockedUserId = blockedUserId;
        CreatedAtUtc = createdAtUtc;
    }
}
