using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Giao đề cho lớp hoặc học sinh. Không có dòng giao thì đề vẫn theo danh sách thí sinh cũ.
public interface IExamAssignmentRepository
{
    Task AddAsync(ExamAssignment assignment, CancellationToken ct);
    Task<ExamAssignment?> FindActiveAsync(long examId, long? classroomId, long? studentId, CancellationToken ct);
    Task<List<ExamAssignment>> ListByClassroomAsync(long classroomId, CancellationToken ct);
    Task<bool> AllowsAsync(long examId, long userId, CancellationToken ct);
}
