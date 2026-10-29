using AIQuizHSG.Application.Omr;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Teacher)]
[Route("api/omr")]
public class OmrController(IOmrService omr) : ControllerBase
{
    [HttpGet("templates")]
    public async Task<ActionResult<IReadOnlyList<OmrTemplateSummary>>> Templates(CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.ListTemplatesAsync(actor, ct));
    }

    [HttpGet("template-styles")]
    public async Task<ActionResult<IReadOnlyList<OmrFormStyleItem>>> TemplateStyles(CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.ListFormStylesAsync(actor, ct));
    }

    [HttpGet("templates/preview")]
    public async Task<IActionResult> PreviewDraft([FromQuery] int questionCount, [FromQuery] int choiceCount, [FromQuery] string? style, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        var result = await omr.PreviewTemplateAsync(null, questionCount, choiceCount, style, actor, ct);
        if (result.Status != Application.Common.OperationStatus.Ok || result.Value is null)
            return ApiResults.From(result);
        return File(result.Value, "image/png");
    }

    [HttpGet("templates/{id:long}/preview")]
    public async Task<IActionResult> Preview(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        var result = await omr.PreviewTemplateAsync(id, null, null, null, actor, ct);
        if (result.Status != Application.Common.OperationStatus.Ok || result.Value is null)
            return ApiResults.From(result);
        return File(result.Value, "image/png");
    }

    [HttpPost("templates")]
    public async Task<ActionResult<OmrTemplateSummary>> CreateTemplate(SaveOmrTemplateRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.CreateTemplateAsync(request, actor, ct));
    }

    [HttpGet("batches")]
    public async Task<ActionResult<IReadOnlyList<OmrBatchSummary>>> Batches(CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.ListBatchesAsync(actor, ct));
    }

    [HttpPost("batches")]
    public async Task<ActionResult<OmrBatchSummary>> CreateBatch(CreateOmrBatchRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.CreateBatchAsync(request, actor, ct));
    }

    [HttpGet("batches/{id:long}")]
    public async Task<ActionResult<OmrBatchSummary>> Batch(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.GetBatchAsync(id, actor, ct));
    }

    [HttpPost("batches/{id:long}/sheets")]
    [RequestSizeLimit(80_000_000)]
    [RequestFormLimits(MultipartBodyLengthLimit = 80_000_000)]
    public async Task<IActionResult> Upload(long id, [FromForm] List<IFormFile> files, [FromForm] string? sourceType, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();

        var payload = new List<OmrUploadFile>();
        foreach (var file in files)
        {
            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory, ct);
            payload.Add(new OmrUploadFile(file.FileName, file.ContentType, memory.ToArray()));
        }

        var result = await omr.UploadAsync(id, sourceType, payload, actor, ct);
        if (result.Status != Application.Common.OperationStatus.Ok)
            return ApiResults.From(result);
        return Accepted(result.Value);
    }

    [HttpGet("batches/{id:long}/sheets")]
    public async Task<ActionResult<OmrSheetPage>> Sheets(long id, [FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.ListSheetsAsync(id, status, page, pageSize, actor, ct));
    }

    [HttpPost("batches/{id:long}/sheets/{sheetId:long}/retry")]
    public async Task<ActionResult<OmrSheetSummary>> Retry(long id, long sheetId, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.RetryAsync(id, sheetId, actor, ct));
    }

    [HttpGet("sheets/{id:long}")]
    public async Task<ActionResult<OmrSheetDetail>> Sheet(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.GetSheetAsync(id, actor, ct));
    }

    [HttpGet("sheets/{id:long}/file")]
    public async Task<IActionResult> File(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        var result = await omr.OpenFileAsync(id, actor, ct);
        if (result.Status != Application.Common.OperationStatus.Ok || result.Value.Content is null)
            return ApiResults.From(result);
        return File(result.Value.Content, result.Value.ContentType, result.Value.FileName);
    }

    [HttpPut("sheets/{id:long}/review")]
    public async Task<ActionResult<OmrSheetDetail>> Review(long id, ReviewOmrSheetRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.ReviewAsync(id, request, actor, ct));
    }

    [HttpPost("batches/{id:long}/grade")]
    public async Task<ActionResult<OmrBatchSummary>> Grade(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.GradeAsync(id, actor, ct));
    }

    [HttpPost("batches/{id:long}/approve")]
    public async Task<ActionResult<OmrBatchSummary>> Approve(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.ApproveAsync(id, actor, ct));
    }

    [HttpPost("batches/{id:long}/publish")]
    public async Task<ActionResult<OmrBatchSummary>> Publish(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.PublishAsync(id, actor, ct));
    }

    [HttpGet("batches/{id:long}/results")]
    public async Task<ActionResult<IReadOnlyList<OmrResultRow>>> Results(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await omr.ResultsAsync(id, actor, ct));
    }

    [HttpGet("batches/{id:long}/export")]
    public async Task<IActionResult> Export(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        var result = await omr.ExportAsync(id, actor, ct);
        if (result.Status != Application.Common.OperationStatus.Ok || result.Value is null)
            return ApiResults.From(result);
        return File(result.Value, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "bang-diem-omr.xlsx");
    }
}
