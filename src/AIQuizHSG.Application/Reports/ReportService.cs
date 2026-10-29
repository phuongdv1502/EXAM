using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;

namespace AIQuizHSG.Application.Reports;

// Trả số liệu tổng hợp. Repository đọc, service không đụng DbContext.
public sealed partial class ReportService : BaseService, IReportService
{
    public ReportService(IUnitOfWork unitOfWork) : base(unitOfWork)
    {
    }

    public Task<DashboardReport> GetDashboardAsync(CancellationToken ct) =>
        _unitOfWork.Reports.LoadDashboardAsync(ct);
}
