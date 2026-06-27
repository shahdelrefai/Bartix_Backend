using Bartrix.Modules.Listings.Application;

namespace Bartrix.Modules.Listings.Infrastructure;

/// <summary>
/// Template-based fallback used when no AI API key is configured.
/// </summary>
public sealed class FallbackAiDescriptionService : IAiDescriptionService
{
    public Task<IReadOnlyList<string>> GetSuggestionsAsync(
        string? imageUrl,
        string? category,
        string? condition,
        CancellationToken cancellationToken)
    {
        var cat  = string.IsNullOrWhiteSpace(category)  ? "item"      : category;
        var cond = string.IsNullOrWhiteSpace(condition) ? "good"      : condition.ToLowerInvariant();

        IReadOnlyList<string> suggestions = new[]
        {
            $"This {cat} is in {cond} condition and works perfectly. A great find for anyone looking for quality at a fair value.",
            $"Well-maintained {cat} available for barter or trade. Condition: {cond}. No defects — ready for its next owner.",
            $"Offering this {cat} in {cond} condition. Ideal for someone looking to trade or exchange for something of equal value."
        };

        return Task.FromResult(suggestions);
    }
}
