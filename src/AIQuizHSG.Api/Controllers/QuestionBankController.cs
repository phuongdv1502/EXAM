using AIQuizHSG.Api;
using AIQuizHSG.Application.Difficulty;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Teacher)]
[Route("api/question-bank")]
// Kiểm tra ngân hàng và chọn câu theo ma trận, chưa ghi đề.
public class QuestionBankController(IExamGenerationService generation) : ControllerBase
{
    [HttpPost("availability")]
    public async Task<ActionResult<AvailabilityResponse>> Availability(AvailabilityRequest request, CancellationToken ct) =>
        ApiResults.From(await generation.CheckAsync(request, ct));

    [HttpPost("draw")]
    public async Task<ActionResult<DrawQuestionsResponse>> Draw(AvailabilityRequest request, CancellationToken ct) =>
        ApiResults.From(await generation.DrawAsync(request, ct));
}
