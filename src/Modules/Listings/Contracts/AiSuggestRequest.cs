namespace Bartrix.Modules.Listings.Contracts;

public sealed record AiSuggestRequest(string? ImageUrl, string? Category, string? Condition);

public sealed record AiSuggestResponse(IReadOnlyList<string> Suggestions);
