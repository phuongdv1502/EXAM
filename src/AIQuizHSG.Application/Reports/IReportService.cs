namespace AIQuizHSG.Application.Reports;

public interface IReportService
{
    Task<DashboardReport> GetDashboardAsync(CancellationToken ct);
}
