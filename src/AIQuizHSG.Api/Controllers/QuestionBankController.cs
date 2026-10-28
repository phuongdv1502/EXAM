using AIQuizHSG.Api;
using AIQuizHSG.Application.Difficulty;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Teacher)]
[Route("api/question-bank")]
// Kiểm tra ngân hàng còn đủ câu cho từng mức trước khi sinh đề.
public class QuestionBankController(IExamGenerationService generation) : ControllerBase
{
    [HttpPost("availability")]
    public async Task<ActionResult<AvailabilityResponse>> Availability(AvailabilityRequest request, CancellationToken ct) =>
        ApiResults.From(await generation.CheckAsync(request, ct));
}
