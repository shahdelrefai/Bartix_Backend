namespace Bartrix.Modules.Auth.Infrastructure;

public sealed record GoogleUserInfo(string Subject, string Email, bool EmailVerified, string? Name);

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken);
}
