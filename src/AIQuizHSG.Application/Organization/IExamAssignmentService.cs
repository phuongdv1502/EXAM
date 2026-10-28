using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Organization;

public interface IExamAssignmentService
{
    Task<OperationResult<IReadOnlyList<ExamAssignmentResponse>>> ListAsync(long examId, Actor actor, CancellationToken ct);
    Task<OperationResult<ExamAssignmentResponse>> AssignAsync(long examId, AssignExamRequest request, Actor actor, CancellationToken ct);
}
