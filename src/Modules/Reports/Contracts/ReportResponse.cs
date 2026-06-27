namespace Bartrix.Modules.Reports.Contracts;

public sealed record ReportResponse(
    Guid Id,
    Guid ReporterId,
    string ReporterName,
    Guid ReportedProductId,
    string ReportedProductTitle,
    Guid ReportedProductOwnerId,
    string Reason,
    string Description,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ReviewedAt,
    string? AdminNote);
