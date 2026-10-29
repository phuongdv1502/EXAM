using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

public interface IOmrRepository
{
    Task<List<OmrFormStyle>> ListFormStylesAsync(CancellationToken ct);
    Task<List<OmrTemplate>> ListTemplatesAsync(CancellationToken ct);
    Task<OmrTemplate?> FindTemplateAsync(long id, CancellationToken ct);
    Task<bool> TemplateCodeExistsAsync(string code, int version, CancellationToken ct);
    Task AddTemplateAsync(OmrTemplate template, CancellationToken ct);

    Task<List<OmrImportBatch>> ListBatchesAsync(CancellationToken ct);
    Task<OmrImportBatch?> FindBatchAsync(long id, CancellationToken ct);
    Task AddBatchAsync(OmrImportBatch batch, CancellationToken ct);

    Task<OmrScannedSheet?> FindSheetByHashAsync(long batchId, string hash, CancellationToken ct);
    Task<OmrScannedSheet?> FindSheetAsync(long id, CancellationToken ct);
    Task<List<OmrScannedSheet>> ListSheetsAsync(long batchId, string? status, int skip, int take, CancellationToken ct);
    Task<int> CountSheetsAsync(long batchId, string? status, CancellationToken ct);
    Task<List<OmrScannedSheet>> ListSheetStatusesAsync(long batchId, CancellationToken ct);
    Task AddSheetAsync(OmrScannedSheet sheet, CancellationToken ct);
    void ReplaceAnswers(OmrScannedSheet sheet, IReadOnlyList<OmrRecognizedAnswer> answers);
    Task<bool> HasOtherGradedStudentAsync(long batchId, long sheetId, string studentCode, CancellationToken ct);
}
