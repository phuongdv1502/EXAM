using AIQuizHSG.Api;
using AIQuizHSG.Application.Exams;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Student)]
[Route("api/attempts")]
// Xem đề, lưu lựa chọn và nộp. Không trả đáp án đúng, cũng chưa trả điểm.
public class AttemptsController(AttemptService attempts) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AttemptSummary>>> Mine(CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return Ok(await attempts.ListMineAsync(actor.UserId, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<AttemptPaper>> Get(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await attempts.GetAsync(id, actor, ct));
    }

    [HttpPut("{id:long}/answers/{examQuestionId:long}")]
    public async Task<ActionResult<AttemptPaper>> Save(
        long id,
        long examQuestionId,
        SaveAnswerRequest request,
        CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await attempts.SaveAnswerAsync(id, examQuestionId, request, actor, ct));
    }

    [HttpPost("{id:long}/submit")]
    public async Task<ActionResult<AttemptSummary>> Submit(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await attempts.SubmitAsync(id, actor, ct));
    }
}
