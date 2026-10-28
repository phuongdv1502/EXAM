using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Hồ sơ học sinh và lịch sử phân lớp.
public interface IStudentRecordRepository
{
    Task<List<StudentProfile>> SearchAsync(long schoolId, string? keyword, CancellationToken ct);
    Task<StudentProfile?> FindAsync(long id, CancellationToken ct);
    Task<StudentProfile?> FindByCodeAsync(long schoolId, string code, CancellationToken ct);
    Task<List<StudentProfile>> FindAllByCodeAsync(string code, CancellationToken ct);
    Task<StudentProfile?> FindByUserAsync(long userId, CancellationToken ct);
    Task AddAsync(StudentProfile student, CancellationToken ct);
    Task<StudentEnrollment?> FindActiveEnrollmentAsync(long studentId, CancellationToken ct);
    Task<List<StudentProfile>> ListActiveInClassroomAsync(long classroomId, CancellationToken ct);
    Task AddEnrollmentAsync(StudentEnrollment enrollment, CancellationToken ct);
}
