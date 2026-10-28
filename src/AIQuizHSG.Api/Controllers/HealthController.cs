using AIQuizHSG.Application.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/health")]
// Kiểm tra API có mở được kết nối MySQL hay không.
public class HealthController(IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken ct)
    {
        var ready = false;
        try
        {
            ready = await unitOfWork.CanConnectAsync(ct);
        }
        catch
        {
            ready = false;
        }

        if (!ready)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { status = "Unavailable", database = "Disconnected" });

        return Ok(new { status = "Healthy", database = "Ready" });
    }
}
