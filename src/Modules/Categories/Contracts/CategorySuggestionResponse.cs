namespace Bartrix.Modules.Categories.Contracts;

public sealed record CategorySuggestionResponse(
    Guid Id,
    string SuggestedName,
    Guid SuggestedBy,
    string SuggestedByName,
    string Status,
    DateTimeOffset CreatedAt,
    Guid? ReviewedBy,
    string? ReviewedByName,
    DateTimeOffset? ReviewedAt);
