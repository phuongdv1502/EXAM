using AIQuizHSG.Api;
using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Difficulty;
using AIQuizHSG.Application.Exams;
using AIQuizHSG.Application.Omr;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Teacher)]
[Route("api/exams")]
// Giáo viên tạo đề, phát hành và đóng kỳ thi.
public class ExamsController(IExamService exams, IExamGenerationService generation, IOmrService omr) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<ExamSummary>> List(CancellationToken ct) =>
        await exams.ListAsync(ct);

    [HttpGet("{id:long}")]
    public async Task<ActionResult<ExamResponse>> Get(long id, CancellationToken ct) =>
        ApiResults.From(await exams.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<ExamResponse>> Create(SaveExamRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await exams.CreateAsync(request, actor, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ExamResponse>> Update(long id, SaveExamRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await exams.UpdateAsync(id, request, actor, ct));
    }

    [HttpPost("generate")]
    public async Task<ActionResult<ExamResponse>> Generate(GenerateExamRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await generation.GenerateAsync(request, actor, ct));
    }

    [HttpPost("{id:long}/validate")]
    public async Task<ActionResult<ExamValidationResponse>> Validate(long id, CancellationToken ct) =>
        ApiResults.From(await generation.ValidateAsync(id, ct));

    [HttpPost("{id:long}/publish")]
    public async Task<ActionResult<ExamResponse>> Publish(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await exams.PublishAsync(id, actor, ct));
    }

    // Mỗi học sinh một trang. Trang web đưa tệp này vào hộp thoại in.
    [HttpGet("{id:long}/sheets")]
    public async Task<IActionResult> Sheets(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        var result = await omr.PrintSheetsAsync(id, actor, ct);
        if (result.Status != OperationStatus.Ok || result.Value is null)
            return ApiResults.From(result);
        return File(result.Value, "application/pdf");
    }

    [HttpPost("{id:long}/close")]
    public async Task<ActionResult<ExamResponse>> Close(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await exams.CloseAsync(id, actor, ct));
    }
}
