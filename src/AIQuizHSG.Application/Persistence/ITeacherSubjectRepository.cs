using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Môn giáo viên được dạy. Repository chỉ đọc hoặc gắn bản ghi.
public interface ITeacherSubjectRepository
{
    Task<List<Subject>> ListAsync(long userId, CancellationToken ct);
    Task<bool> AllowsAsync(long userId, long subjectId, CancellationToken ct);
    Task ReplaceAsync(long userId, IReadOnlyCollection<long> subjectIds, CancellationToken ct);
}
