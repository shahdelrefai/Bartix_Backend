namespace Bartrix.Modules.Reports.Contracts;

public sealed record SubmitReportRequest(
    Guid ReportedProductId,
    string ReportedProductTitle,
    Guid ReportedProductOwnerId,
    string Reason,
    string Description = "",
    string ReporterName = "");
