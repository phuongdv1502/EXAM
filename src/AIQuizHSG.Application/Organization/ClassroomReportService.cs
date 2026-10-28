using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Organization;

public interface IClassroomReportService
{
    Task<OperationResult<IReadOnlyList<ClassroomExamResult>>> ExamResultsAsync(long classroomId, Actor actor, CancellationToken ct);
}

// Báo cáo theo lớp từ các đề đã giao. Điểm giữ nguyên giá trị đã lưu, chưa tự tính.
public sealed partial class ClassroomReportService : OrganizationService, IClassroomReportService
{
    public ClassroomReportService(IUnitOfWork unitOfWork, ISchoolAccess access) : base(unitOfWork, access)
    {
    }

    public async Task<OperationResult<IReadOnlyList<ClassroomExamResult>>> ExamResultsAsync(long classroomId, Actor actor, CancellationToken ct)
    {
        var room = await unitOfWork.Classrooms.FindAsync(classroomId, ct);
        if (room is null)
            return OperationResult<IReadOnlyList<ClassroomExamResult>>.NotFound(Messages.ClassroomNotFound);
        if (await access.DenyClassroomAsync(actor.UserId, room, "report.view", ct) is { } denied)
            return OperationResult<IReadOnlyList<ClassroomExamResult>>.Forbidden(denied);

        var students = await unitOfWork.StudentRecords.ListActiveInClassroomAsync(classroomId, ct);
        var assignments = await unitOfWork.ExamAssignments.ListByClassroomAsync(classroomId, ct);
        var examIds = assignments.Select(item => item.ExamId).ToHashSet();
        var userIds = students.Where(student => student.UserId is not null).Select(student => student.UserId!.Value).ToArray();
        var attempts = await unitOfWork.Attempts.ListByUserIdsAsync(userIds, ct);

        var lines = students.Select(student =>
        {
            var mine = attempts
                .Where(attempt => student.UserId is not null && attempt.UserId == student.UserId && examIds.Contains(attempt.ExamId))
                .GroupBy(attempt => attempt.ExamId)
                .Select(group => group.OrderByDescending(attempt => attempt.AttemptNumber).First())
                .Select(attempt => new StudentExamLine(attempt.ExamId, attempt.Exam.Title, attempt.Status.ToString(), attempt.Score, attempt.MaxScore))
                .ToArray();
            return new ClassroomExamResult(student.Id, student.StudentCode, student.FullName, mine);
        }).ToArray();

        return OperationResult<IReadOnlyList<ClassroomExamResult>>.Ok(lines);
    }
}
