using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Exams;

namespace AIQuizHSG.Application.Difficulty;

public interface IExamGenerationService
{
    Task<OperationResult<AvailabilityResponse>> CheckAsync(AvailabilityRequest request, CancellationToken ct);
    Task<OperationResult<DrawQuestionsResponse>> DrawAsync(AvailabilityRequest request, CancellationToken ct);
    Task<OperationResult<ExamResponse>> GenerateAsync(GenerateExamRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<ExamValidationResponse>> ValidateAsync(long examId, CancellationToken ct);
}
