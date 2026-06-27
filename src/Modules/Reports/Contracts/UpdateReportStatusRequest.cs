namespace Bartrix.Modules.Reports.Contracts;

public sealed record UpdateReportStatusRequest(string Status, string? AdminNote = null);
