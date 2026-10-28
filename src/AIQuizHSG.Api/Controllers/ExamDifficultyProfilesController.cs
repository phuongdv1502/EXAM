using AIQuizHSG.Api;
using AIQuizHSG.Application.Difficulty;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Teacher)]
[Route("api/exam-difficulty-profiles")]
// Ma trận độ khó của cả đề.
public class ExamDifficultyProfilesController(ExamProfileService profiles) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ExamProfileResponse>> List(CancellationToken ct) =>
        await profiles.ListAsync(ct);

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ExamProfileResponse>> Get(long id, CancellationToken ct) =>
        ApiResults.From(await profiles.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ExamProfileResponse>> Create(SaveExamProfileRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await profiles.CreateAsync(request, actor, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ExamProfileResponse>> Update(long id, SaveExamProfileRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await profiles.UpdateAsync(id, request, actor, ct));
    }

    [HttpPost("{id:long}/preview")]
    public async Task<ActionResult<ProfilePreviewResponse>> Preview(long id, PreviewProfileRequest request, CancellationToken ct) =>
        ApiResults.From(await profiles.PreviewAsync(id, request, ct));
}
