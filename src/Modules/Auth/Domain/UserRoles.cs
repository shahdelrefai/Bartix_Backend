namespace Bartrix.Modules.Auth.Domain;

/// <summary>
/// Role string constants mirroring the Flutter <c>UserRole</c> enum
/// (user / admin / moderator / premium / agent).
/// </summary>
public static class UserRoles
{
    public const string User = "user";
    public const string Admin = "admin";
    public const string Moderator = "moderator";
    public const string Premium = "premium";
    public const string Agent = "agent";

    public static readonly IReadOnlyList<string> All = new[] { User, Admin, Moderator, Premium, Agent };

    public static bool IsValid(string? role) =>
        role is not null && All.Contains(role, StringComparer.OrdinalIgnoreCase);
}
