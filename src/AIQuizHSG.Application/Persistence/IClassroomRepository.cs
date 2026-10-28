using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Lớp học và phân công giáo viên bộ môn.
public interface IClassroomRepository
{
    Task<List<Classroom>> ListAsync(long schoolId, long? schoolYearId, CancellationToken ct);
    Task<Classroom?> FindAsync(long id, CancellationToken ct);
    Task<Classroom?> FindByCodeAsync(long schoolYearId, string classCode, CancellationToken ct);
    Task AddAsync(Classroom classroom, CancellationToken ct);
    Task<int> CountActiveStudentsAsync(long classroomId, CancellationToken ct);
    Task<bool> TeachesAsync(long classroomId, long userId, CancellationToken ct);
    Task<ClassMembership?> FindSubjectTeacherAsync(long classroomId, long userId, long subjectId, CancellationToken ct);
    Task<List<ClassMembership>> ListTeachersAsync(long classroomId, CancellationToken ct);
    Task AddTeacherAsync(ClassMembership membership, CancellationToken ct);
}
