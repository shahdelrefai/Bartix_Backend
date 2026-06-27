namespace Bartrix.Modules.Auth.Infrastructure;

public sealed class GoogleAuthOptions
{
    public const string SectionName = "Authentication:Google";

    /// <summary>
    /// Accepted OAuth client IDs (the token's <c>aud</c> claim must match one of these).
    /// Includes the web/server client ID used by the Flutter app. When empty, the
    /// audience check is skipped (development only).
    /// </summary>
    public string[] AllowedAudiences { get; set; } = Array.Empty<string>();
}
