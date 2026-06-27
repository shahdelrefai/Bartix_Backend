namespace Bartrix.Modules.Listings.Application;

public interface IAiDescriptionService
{
    Task<IReadOnlyList<string>> GetSuggestionsAsync(string? imageUrl, string? category, string? condition, CancellationToken cancellationToken);
}
