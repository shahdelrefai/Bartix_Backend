using Bartrix.BuildingBlocks.Notifications;
using Bartrix.Modules.Reports.Contracts;
using Bartrix.Modules.Reports.Domain;
using Bartrix.Modules.Reports.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Bartrix.Modules.Reports.Application;

public sealed class ReportsService : IReportsService
{
    private readonly ReportsDbContext _dbContext;
    private readonly IAdminUserReader _adminUserReader;
    private readonly INotificationPublisher _notifications;
    private readonly TimeProvider _timeProvider;

    public ReportsService(
        ReportsDbContext dbContext,
        IAdminUserReader adminUserReader,
        INotificationPublisher notifications,
        TimeProvider timeProvider)
    {
        _dbContext = dbContext;
        _adminUserReader = adminUserReader;
        _notifications = notifications;
        _timeProvider = timeProvider;
    }

    public async Task<ReportResponse> SubmitAsync(Guid reporterId, SubmitReportRequest request, CancellationToken cancellationToken)
    {
        if (reporterId == request.ReportedProductOwnerId)
            throw new ReportsValidationException("You cannot report your own product.");

        var report = new Report(
            reporterId,
            request.ReporterName,
            request.ReportedProductId,
            request.ReportedProductTitle,
            request.ReportedProductOwnerId,
            request.Reason,
            request.Description,
            _timeProvider.GetUtcNow());

        _dbContext.Reports.Add(report);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var adminIds = await _adminUserReader.GetAdminUserIdsAsync(cancellationToken);
        foreach (var adminId in adminIds)
        {
            await _notifications.PublishAsync(new NotificationPublishRequest(
                UserId: adminId,
                TitleKey: "newReportTitle",
                BodyKey: "newReportBody",
                Type: "system",
                BodyArgs: new Dictionary<string, string>
                {
                    ["reporter"] = request.ReporterName,
                    ["product"] = request.ReportedProductTitle
                },
                RelatedId: report.Id.ToString()), cancellationToken);
        }

        return Map(report);
    }

    public async Task<IReadOnlyList<ReportResponse>> GetAllAsync(CancellationToken cancellationToken)
    {
        var reports = await _dbContext.Reports
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        return reports.Select(Map).ToList();
    }

    public async Task UpdateStatusAsync(Guid reportId, UpdateReportStatusRequest request, CancellationToken cancellationToken)
    {
        if (!Report.Statuses.IsValid(request.Status))
            throw new ReportsValidationException($"Invalid status '{request.Status}'.");

        var report = await _dbContext.Reports
            .SingleOrDefaultAsync(x => x.Id == reportId, cancellationToken)
            ?? throw new ReportsValidationException("Report not found.");

        report.UpdateStatus(request.Status, request.AdminNote, _timeProvider.GetUtcNow());
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static ReportResponse Map(Report r) =>
        new(r.Id, r.ReporterId, r.ReporterName, r.ReportedProductId,
            r.ReportedProductTitle, r.ReportedProductOwnerId, r.Reason,
            r.Description, r.Status, r.CreatedAtUtc, r.ReviewedAtUtc, r.AdminNote);
}
