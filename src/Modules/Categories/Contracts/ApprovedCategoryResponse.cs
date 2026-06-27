namespace Bartrix.Modules.Categories.Contracts;

public sealed record ApprovedCategoryResponse(
    Guid Id,
    string Name,
    Guid AddedBy,
    string AddedByName,
    DateTimeOffset AddedAt);
