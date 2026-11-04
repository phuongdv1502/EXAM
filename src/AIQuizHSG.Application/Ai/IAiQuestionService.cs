using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Ai;

public interface IAiQuestionService
{
    Task<OperationResult<GenerateAiQuestionsResult>> GenerateAsync(
        GenerateAiQuestionsRequest request,
        Actor actor,
        CancellationToken ct);
}
