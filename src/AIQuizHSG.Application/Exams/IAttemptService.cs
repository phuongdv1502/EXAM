using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Exams;

public interface IAttemptService
{
    Task<IReadOnlyList<ExamSummary>> ListOpenAsync(long userId, CancellationToken ct);
    Task<IReadOnlyList<AttemptSummary>> ListMineAsync(long userId, CancellationToken ct);
    Task<OperationResult<AttemptPaper>> StartAsync(long examId, Actor actor, CancellationToken ct);
    Task<OperationResult<AttemptPaper>> GetAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<AttemptPaper>> SaveAnswerAsync(long id, long examQuestionId, SaveAnswerRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<AttemptSummary>> SubmitAsync(long id, Actor actor, CancellationToken ct);
}
