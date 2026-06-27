using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Bartrix.Modules.Auth.Infrastructure;

/// <summary>
/// Validates a Google ID token by calling Google's public tokeninfo endpoint and
/// checking the audience. This avoids taking a dependency on the Google Auth SDK while
/// still verifying the token signature/expiry server-side (Google validates those).
/// </summary>
public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private const string TokenInfoEndpoint = "https://oauth2.googleapis.com/tokeninfo?id_token=";

    private readonly HttpClient _httpClient;
    private readonly GoogleAuthOptions _options;

    public GoogleTokenValidator(HttpClient httpClient, IOptions<GoogleAuthOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        TokenInfoPayload? payload;
        try
        {
            payload = await _httpClient.GetFromJsonAsync<TokenInfoPayload>(
                TokenInfoEndpoint + Uri.EscapeDataString(idToken),
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            return null;
        }

        if (payload is null || string.IsNullOrWhiteSpace(payload.Subject) || string.IsNullOrWhiteSpace(payload.Email))
        {
            return null;
        }

        if (_options.AllowedAudiences.Length > 0 &&
            !_options.AllowedAudiences.Contains(payload.Audience, StringComparer.Ordinal))
        {
            return null;
        }

        var emailVerified = string.Equals(payload.EmailVerified, "true", StringComparison.OrdinalIgnoreCase);
        return new GoogleUserInfo(payload.Subject, payload.Email, emailVerified, payload.Name);
    }

    private sealed record TokenInfoPayload(
        [property: JsonPropertyName("sub")] string Subject,
        [property: JsonPropertyName("aud")] string Audience,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("email_verified")] string? EmailVerified,
        [property: JsonPropertyName("name")] string? Name);
}
