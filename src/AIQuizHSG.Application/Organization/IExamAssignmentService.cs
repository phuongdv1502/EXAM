using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Organization;

public interface IExamAssignmentService
{
    Task<OperationResult<ExamAssignmentResponse>> AssignAsync(long examId, AssignExamRequest request, Actor actor, CancellationToken ct);
}
