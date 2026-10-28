using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Lượt làm bài và phương án đã chọn. Không tính điểm.
public interface IAttemptRepository
{
    Task<int> CountAsync(long examId, long userId, CancellationToken ct);
    Task<ExamAttempt?> FindInProgressAsync(long examId, long userId, CancellationToken ct);
    Task<ExamAttempt?> FindAsync(long id, CancellationToken ct);
    Task<ExamAttempt?> FindLatestAsync(long examId, long userId, CancellationToken ct);
    Task<List<ExamAttempt>> ListByUserAsync(long userId, CancellationToken ct);
    Task<List<ExamAttempt>> ListByUserIdsAsync(IReadOnlyCollection<long> userIds, CancellationToken ct);
    Task AddAsync(ExamAttempt attempt, CancellationToken ct);
    void ReplaceSelections(AttemptAnswer answer, IReadOnlyList<long> optionIds);
}
