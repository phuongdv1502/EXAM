using AIQuizHSG.Api;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Teacher)]
[Route("api/subjects")]
// Giáo viên và quản trị xem, thêm, sửa môn học.
public class SubjectsController(CatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<SubjectResponse>> List(CancellationToken ct) =>
        await catalog.ListSubjectsAsync(ct);

    [HttpPost]
    public async Task<ActionResult<SubjectResponse>> Create([FromBody] SaveSubjectRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await catalog.CreateSubjectAsync(request, actor, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<SubjectResponse>> Update(long id, [FromBody] SaveSubjectRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await catalog.UpdateSubjectAsync(id, request, actor, ct));
    }
}
