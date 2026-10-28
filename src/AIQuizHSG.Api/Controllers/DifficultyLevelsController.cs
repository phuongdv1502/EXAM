using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Teacher)]
[Route("api/difficulty-levels")]
// Năm mức độ của câu hỏi.
public class DifficultyLevelsController(IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<object>> List(CancellationToken ct)
    {
        var levels = await unitOfWork.DifficultyLevels.ListActiveAsync(ct);
        return levels.Select(level => new
        {
            id = level.Id,
            code = level.Code,
            name = level.Name,
            description = level.Description,
            sortOrder = level.SortOrder
        }).ToArray();
    }
}
