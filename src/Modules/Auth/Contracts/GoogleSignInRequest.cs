namespace Bartrix.Modules.Auth.Contracts;

/// <summary>
/// Carries the Google-issued ID token obtained client-side by the Flutter
/// <c>google_sign_in</c> plugin. The backend validates it and issues its own JWT.
/// </summary>
public sealed record GoogleSignInRequest(string IdToken);
