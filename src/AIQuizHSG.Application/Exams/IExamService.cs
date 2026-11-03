using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Exams;

public interface IExamService
{
    Task<IReadOnlyList<ExamSummary>> ListAsync(CancellationToken ct);
    Task<OperationResult<ExamResponse>> GetAsync(long id, CancellationToken ct);
    Task<OperationResult<ExamResponse>> CreateAsync(SaveExamRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<ExamResponse>> UpdateAsync(long id, SaveExamRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<ExamResponse>> PublishAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<ExamResponse>> CloseAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<ExamResponse>> AttachSheetAsync(long id, AttachExamSheetRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<ExamVariantItem>>> ListVariantsAsync(long examId, Actor actor, CancellationToken ct);
    Task<OperationResult<IReadOnlyList<ExamVariantItem>>> GenerateVariantsAsync(long examId, GenerateExamVariantsRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<byte[]>> PrintVariantPaperAsync(long examId, string code, Actor actor, CancellationToken ct);
    Task<OperationResult<(byte[] Pdf, string Code)>> PrintPaperAsync(long examId, Actor actor, CancellationToken ct);
}
