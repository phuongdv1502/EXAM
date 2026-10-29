using AIQuizHSG.Api;
using AIQuizHSG.Application.Accounts;
using AIQuizHSG.Application.Questions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

public sealed class ReplaceTeachingSubjectsRequest
{
    public List<long> SubjectIds { get; set; } = [];
}

[ApiController]
[Authorize]
[Route("api/teaching-subjects")]
// Môn giáo viên được dạy, và môn người đang đăng nhập được dùng khi tạo nội dung.
public class TeachingSubjectsController(ITeachingSubjectService teaching) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SubjectResponse>>> Mine(CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return Ok(await teaching.ListMineAsync(actor, ct));
    }

    [HttpGet("catalog")]
    public async Task<ActionResult<IReadOnlyList<SubjectResponse>>> Catalog(CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await teaching.ListCatalogAsync(actor, ct));
    }

    [HttpGet("users/{userId:long}")]
    public async Task<ActionResult<IReadOnlyList<SubjectResponse>>> ForUser(long userId, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await teaching.ListForUserAsync(userId, actor, ct));
    }

    [HttpPut("users/{userId:long}")]
    public async Task<ActionResult<IReadOnlyList<SubjectResponse>>> Replace(long userId, ReplaceTeachingSubjectsRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await teaching.ReplaceAsync(userId, request.SubjectIds ?? [], actor, ct));
    }
}
