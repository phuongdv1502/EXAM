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
}
