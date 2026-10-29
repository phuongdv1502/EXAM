using AIQuizHSG.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = "Staff")]
[Route("api/reports")]
// Số liệu cho màn tổng quan và các báo cáo. Học sinh không vào đây.
public class ReportsController(IReportService reports) : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<DashboardReport> Dashboard(CancellationToken ct) =>
        await reports.GetDashboardAsync(ct);
}
