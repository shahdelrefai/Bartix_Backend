using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Bartrix.Modules.Listings.Application;
using Microsoft.Extensions.Logging;

namespace Bartrix.Modules.Listings.Infrastructure;

/// <summary>
/// Uses OpenAI GPT-4o to generate listing descriptions from an image + context.
/// Falls back gracefully when the service is unavailable.
/// </summary>
public sealed class OpenAiDescriptionService : IAiDescriptionService
{
    private readonly HttpClient _http;
    private readonly string _apiKey;
    private readonly ILogger<OpenAiDescriptionService> _logger;
    private readonly FallbackAiDescriptionService _fallback = new();

    public OpenAiDescriptionService(HttpClient http, string apiKey, ILogger<OpenAiDescriptionService> logger)
    {
        _http = http;
        _apiKey = apiKey;
        _logger = logger;
        _http.BaseAddress = new Uri("https://api.openai.com/v1/");
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
    }

    public async Task<IReadOnlyList<string>> GetSuggestionsAsync(
        string? imageUrl,
        string? category,
        string? condition,
        CancellationToken cancellationToken)
    {
        try
        {
            var prompt = $"You are helping a user create a marketplace listing. " +
                         $"Generate exactly 3 short product descriptions (1-2 sentences each) for a '{category ?? "item"}' " +
                         $"in '{condition ?? "good"}' condition. " +
                         (imageUrl is not null ? "Use the image for additional context. " : "") +
                         "Return a JSON array of 3 strings, nothing else.";

            var messages = imageUrl is not null
                ? new object[]
                  {
                      new { role = "user", content = new object[]
                      {
                          new { type = "text",      text  = prompt },
                          new { type = "image_url", image_url = new { url = imageUrl } }
                      }}
                  }
                : new object[]
                  {
                      new { role = "user", content = prompt }
                  };

            var body = new { model = "gpt-4o", messages, max_tokens = 300 };
            var resp = await _http.PostAsJsonAsync("chat/completions", body, cancellationToken);
            resp.EnsureSuccessStatusCode();

            var json = await resp.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: cancellationToken);
            var text = json
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "[]";

            var suggestions = JsonSerializer.Deserialize<List<string>>(text.Trim()) ?? new List<string>();
            if (suggestions.Count == 0)
                return await _fallback.GetSuggestionsAsync(imageUrl, category, condition, cancellationToken);

            return suggestions.Take(3).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OpenAI description service failed; using fallback.");
            return await _fallback.GetSuggestionsAsync(imageUrl, category, condition, cancellationToken);
        }
    }
}
