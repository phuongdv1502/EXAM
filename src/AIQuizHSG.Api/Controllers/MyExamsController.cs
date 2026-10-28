using AIQuizHSG.Api;
using AIQuizHSG.Application.Exams;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Student)]
[Route("api/my-exams")]
// Đề học sinh được vào và lượt làm đang mở.
public class MyExamsController(IAttemptService attempts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ExamSummary>>> List(CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return Ok(await attempts.ListOpenAsync(actor.UserId, ct));
    }

    [HttpPost("{id:long}/attempts")]
    public async Task<ActionResult<AttemptPaper>> Start(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await attempts.StartAsync(id, actor, ct));
    }
}
