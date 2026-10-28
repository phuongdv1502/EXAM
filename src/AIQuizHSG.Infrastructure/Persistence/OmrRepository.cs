using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

public sealed class OmrRepository(AiQuizDbContext db) : IOmrRepository
{
    public Task<List<OmrTemplate>> ListTemplatesAsync(CancellationToken ct) =>
        db.OmrTemplates.AsNoTracking().OrderBy(item => item.Code).ThenByDescending(item => item.Version).ToListAsync(ct);

    public Task<OmrTemplate?> FindTemplateAsync(long id, CancellationToken ct) =>
        db.OmrTemplates.FirstOrDefaultAsync(item => item.Id == id, ct);

    public Task<bool> TemplateCodeExistsAsync(string code, int version, CancellationToken ct) =>
        db.OmrTemplates.AnyAsync(item => item.Code == code && item.Version == version, ct);

    public async Task AddTemplateAsync(OmrTemplate template, CancellationToken ct) =>
        await db.OmrTemplates.AddAsync(template, ct);

    public Task<List<OmrImportBatch>> ListBatchesAsync(CancellationToken ct) =>
        db.OmrImportBatches.AsNoTracking()
            .Include(batch => batch.Exam)
            .Include(batch => batch.Template)
            .Include(batch => batch.Sheets)
            .OrderByDescending(batch => batch.CreatedAtUtc)
            .ThenByDescending(batch => batch.Id)
            .ToListAsync(ct);

    public Task<OmrImportBatch?> FindBatchAsync(long id, CancellationToken ct) =>
        db.OmrImportBatches
            .Include(batch => batch.Exam)
            .ThenInclude(exam => exam.Questions)
            .ThenInclude(question => question.Options)
            .Include(batch => batch.Template)
            .Include(batch => batch.Sheets)
            .FirstOrDefaultAsync(batch => batch.Id == id, ct);

    public async Task AddBatchAsync(OmrImportBatch batch, CancellationToken ct) =>
        await db.OmrImportBatches.AddAsync(batch, ct);

    public Task<OmrScannedSheet?> FindSheetByHashAsync(long batchId, string hash, CancellationToken ct) =>
        db.OmrScannedSheets.AsNoTracking().FirstOrDefaultAsync(sheet => sheet.BatchId == batchId && sheet.FileHash == hash, ct);

    public Task<OmrScannedSheet?> FindSheetAsync(long id, CancellationToken ct) =>
        Detail().FirstOrDefaultAsync(sheet => sheet.Id == id, ct);

    public Task<List<OmrScannedSheet>> ListSheetsAsync(long batchId, string? status, int skip, int take, CancellationToken ct)
    {
        var query = Detail().Where(sheet => sheet.BatchId == batchId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OmrSheetStatus>(status, true, out var parsed))
            query = query.Where(sheet => sheet.Status == parsed);
        return query.OrderBy(sheet => sheet.Id).Skip(skip).Take(take).ToListAsync(ct);
    }

    public Task<int> CountSheetsAsync(long batchId, string? status, CancellationToken ct)
    {
        var query = db.OmrScannedSheets.Where(sheet => sheet.BatchId == batchId);
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<OmrSheetStatus>(status, true, out var parsed))
            query = query.Where(sheet => sheet.Status == parsed);
        return query.CountAsync(ct);
    }

    public Task<List<OmrScannedSheet>> ListSheetStatusesAsync(long batchId, CancellationToken ct) =>
        db.OmrScannedSheets.Where(sheet => sheet.BatchId == batchId).ToListAsync(ct);

    public async Task AddSheetAsync(OmrScannedSheet sheet, CancellationToken ct) =>
        await db.OmrScannedSheets.AddAsync(sheet, ct);

    public void ReplaceAnswers(OmrScannedSheet sheet, IReadOnlyList<OmrRecognizedAnswer> answers)
    {
        db.OmrRecognizedAnswers.RemoveRange(sheet.Answers);
        sheet.Answers.Clear();
        foreach (var answer in answers)
            sheet.Answers.Add(answer);
    }

    // Phiếu khác đã chấm cùng mã học sinh trong đợt thì không tạo thêm lượt thi.
    public Task<bool> HasOtherGradedStudentAsync(long batchId, long sheetId, string studentCode, CancellationToken ct) =>
        db.OmrScannedSheets.AnyAsync(sheet =>
            sheet.BatchId == batchId
            && sheet.Id != sheetId
            && sheet.Status == OmrSheetStatus.Graded
            && (sheet.ConfirmedStudentCode == studentCode || sheet.RecognizedStudentCode == studentCode), ct);

    private IQueryable<OmrScannedSheet> Detail() =>
        db.OmrScannedSheets
            .Include(sheet => sheet.Batch)
            .ThenInclude(batch => batch.Exam)
            .ThenInclude(exam => exam.Questions)
            .ThenInclude(question => question.Options)
            .Include(sheet => sheet.Batch)
            .ThenInclude(batch => batch.Template)
            .Include(sheet => sheet.Answers)
            .Include(sheet => sheet.ExamAttempt)
            .ThenInclude(attempt => attempt!.Answers)
            .ThenInclude(answer => answer.SelectedOptions);
}
