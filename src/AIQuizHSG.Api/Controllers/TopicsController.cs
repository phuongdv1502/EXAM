using AIQuizHSG.Api;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Teacher)]
[Route("api/topics")]
// Giáo viên và quản trị xem, thêm, sửa chuyên đề. Có thể lọc theo môn.
public class TopicsController(CatalogService catalog) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<TopicResponse>> List([FromQuery] long? subjectId, CancellationToken ct) =>
        await catalog.ListTopicsAsync(subjectId, ct);

    [HttpPost]
    public async Task<ActionResult<TopicResponse>> Create([FromBody] SaveTopicRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await catalog.CreateTopicAsync(request, actor, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<TopicResponse>> Update(long id, [FromBody] SaveTopicRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await catalog.UpdateTopicAsync(id, request, actor, ct));
    }
}
