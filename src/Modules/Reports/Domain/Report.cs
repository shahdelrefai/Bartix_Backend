namespace Bartrix.Modules.Reports.Domain;

public sealed class Report
{
    public Guid Id { get; private set; }
    public Guid ReporterId { get; private set; }
    public string ReporterName { get; private set; } = string.Empty;
    public Guid ReportedProductId { get; private set; }
    public string ReportedProductTitle { get; private set; } = string.Empty;
    public Guid ReportedProductOwnerId { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Status { get; private set; } = Statuses.Pending;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReviewedAtUtc { get; private set; }
    public string? AdminNote { get; private set; }

    public static class Statuses
    {
        public const string Pending = "pending";
        public const string Reviewed = "reviewed";
        public const string Actioned = "actioned";
        public const string Dismissed = "dismissed";

        public static bool IsValid(string? s) =>
            s is Pending or Reviewed or Actioned or Dismissed;
    }

    public Report(
        Guid reporterId,
        string reporterName,
        Guid reportedProductId,
        string reportedProductTitle,
        Guid reportedProductOwnerId,
        string reason,
        string description,
        DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        ReporterId = reporterId;
        ReporterName = reporterName;
        ReportedProductId = reportedProductId;
        ReportedProductTitle = reportedProductTitle;
        ReportedProductOwnerId = reportedProductOwnerId;
        Reason = reason;
        Description = description;
        CreatedAtUtc = createdAtUtc;
    }

    public void UpdateStatus(string status, string? adminNote, DateTimeOffset reviewedAt)
    {
        Status = status;
        AdminNote = adminNote;
        ReviewedAtUtc = reviewedAt;
    }

    private Report() { }
}
