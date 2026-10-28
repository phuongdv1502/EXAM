using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Omr;

public interface IOmrService
{
    Task<OperationResult<IReadOnlyList<OmrTemplateSummary>>> ListTemplatesAsync(Actor actor, CancellationToken ct);
    Task<OperationResult<OmrTemplateSummary>> CreateTemplateAsync(SaveOmrTemplateRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<OmrBatchSummary>>> ListBatchesAsync(Actor actor, CancellationToken ct);
    Task<OperationResult<OmrBatchSummary>> CreateBatchAsync(CreateOmrBatchRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<OmrBatchSummary>> GetBatchAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<UploadOmrSheetsResult>> UploadAsync(long batchId, string? sourceType, IReadOnlyList<OmrUploadFile> files, Actor actor, CancellationToken ct);
    Task<OperationResult<OmrSheetPage>> ListSheetsAsync(long batchId, string? status, int page, int pageSize, Actor actor, CancellationToken ct);
    Task<OperationResult<OmrSheetDetail>> GetSheetAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<(byte[] Content, string ContentType, string FileName)>> OpenFileAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<OmrSheetDetail>> ReviewAsync(long id, ReviewOmrSheetRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<OmrSheetSummary>> RetryAsync(long batchId, long sheetId, Actor actor, CancellationToken ct);
    Task ProcessSheetAsync(long sheetId, CancellationToken ct);
    Task<OperationResult<OmrBatchSummary>> GradeAsync(long batchId, Actor actor, CancellationToken ct);
    Task<OperationResult<OmrBatchSummary>> ApproveAsync(long batchId, Actor actor, CancellationToken ct);
    Task<OperationResult<OmrBatchSummary>> PublishAsync(long batchId, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<OmrResultRow>>> ResultsAsync(long batchId, Actor actor, CancellationToken ct);
    Task<OperationResult<byte[]>> ExportAsync(long batchId, Actor actor, CancellationToken ct);
}

public sealed record OmrUploadFile(string FileName, string ContentType, byte[] Content);
