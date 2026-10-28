using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Organization;

public interface IClassroomReportService
{
    Task<OperationResult<IReadOnlyList<ClassroomExamResult>>> ExamResultsAsync(long classroomId, Actor actor, CancellationToken ct);
}
