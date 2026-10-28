using AIQuizHSG.Api;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Teacher)]
[Route("api/questions")]
// Giáo viên và quản trị quản lý ngân hàng câu hỏi.
public class QuestionsController(QuestionService questions) : ControllerBase
{
    [HttpGet]
    public async Task<QuestionPage> Search(
        [FromQuery] long? subjectId,
        [FromQuery] long? topicId,
        [FromQuery] QuestionLevel? level,
        [FromQuery] byte? difficultyLevelId,
        [FromQuery] QuestionStatus? status,
        [FromQuery] string? keyword,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default) =>
        await questions.SearchAsync(new QuestionFilter
        {
            SubjectId = subjectId,
            TopicId = topicId,
            Level = level,
            DifficultyLevelId = difficultyLevelId,
            Status = status,
            Keyword = keyword,
            Page = page,
            PageSize = pageSize
        }, ct);

    [HttpGet("{id:long}")]
    public async Task<ActionResult<QuestionResponse>> Get(long id, CancellationToken ct) =>
        ApiResults.From(await questions.GetAsync(id, ct));

    [HttpPost]
    public async Task<ActionResult<QuestionResponse>> Create([FromBody] SaveQuestionRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await questions.CreateAsync(request, actor, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<QuestionResponse>> Update(long id, [FromBody] SaveQuestionRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await questions.UpdateAsync(id, request, actor, ct));
    }

    [HttpPost("{id:long}/submit")]
    public async Task<ActionResult<QuestionResponse>> Submit(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await questions.SubmitAsync(id, actor, ct));
    }

    [HttpPost("{id:long}/approve")]
    public async Task<ActionResult<QuestionResponse>> Approve(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await questions.ApproveAsync(id, actor, ct));
    }

    [HttpPost("{id:long}/reject")]
    public async Task<ActionResult<QuestionResponse>> Reject(long id, [FromBody] RejectQuestionRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await questions.RejectAsync(id, request, actor, ct));
    }

    [HttpGet("import-template")]
    public IActionResult Template() =>
        File(questions.CreateImportTemplate(), QuestionExcel.ContentType, QuestionExcel.FileName);

    // Nhận một file .xlsx tối đa 5 MB theo đúng mẫu import-template.
    [HttpPost("import")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ImportResult>> Import(IFormFile? file, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(new { message = Messages.ExcelNotSelected });
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = Messages.ExcelWrongExtension });

        await using var stream = file.OpenReadStream();
        return ApiResults.From(await questions.ImportAsync(stream, actor, ct));
    }
}
