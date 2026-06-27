using Bartrix.Modules.Reports.Contracts;

namespace Bartrix.Modules.Reports.Application;

public interface IReportsService
{
    Task<ReportResponse> SubmitAsync(Guid reporterId, SubmitReportRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<ReportResponse>> GetAllAsync(CancellationToken cancellationToken);
    Task UpdateStatusAsync(Guid reportId, UpdateReportStatusRequest request, CancellationToken cancellationToken);
}
