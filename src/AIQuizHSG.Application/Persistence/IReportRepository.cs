using AIQuizHSG.Application.Reports;

namespace AIQuizHSG.Application.Persistence;

// Đọc số liệu đã gom cho biểu đồ. Không ghi dữ liệu.
public interface IReportRepository
{
    Task<DashboardReport> LoadDashboardAsync(CancellationToken ct);
}
