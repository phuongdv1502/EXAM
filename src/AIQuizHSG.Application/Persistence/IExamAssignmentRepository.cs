using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Giao đề cho cả lớp hoặc một hồ sơ học sinh.
public interface IExamAssignmentRepository
{
    Task AddAsync(ExamAssignment assignment, CancellationToken ct);
    Task<ExamAssignment?> FindActiveAsync(long examId, long? classroomId, long? studentId, CancellationToken ct);
    Task<List<ExamAssignment>> ListByClassroomAsync(long classroomId, CancellationToken ct);
    Task<List<ExamAssignment>> ListByExamAsync(long examId, CancellationToken ct);
    Task<bool> AllowsAsync(long examId, long userId, CancellationToken ct);
}
