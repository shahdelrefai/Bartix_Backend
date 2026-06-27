namespace Bartrix.Modules.Admin.Contracts;

public sealed record AdminListingResponse(
    Guid Id,
    Guid OwnerUserId,
    string Title,
    string Category,
    string Status,
    bool IsActive,
    decimal? AskingPrice,
    string? Condition,
    DateTimeOffset CreatedAtUtc);
