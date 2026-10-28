using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Profile độ khó của đề và cơ cấu từng mức.
public interface IExamProfileRepository
{
    Task<bool> AnySystemDefaultAsync(CancellationToken ct);
    Task<List<ExamDifficultyProfile>> ListAsync(CancellationToken ct);
    Task<ExamDifficultyProfile?> FindAsync(long id, CancellationToken ct);
    Task<bool> IsUsedAsync(long id, CancellationToken ct);
    Task AddAsync(ExamDifficultyProfile profile, CancellationToken ct);
    void ReplaceLevels(ExamDifficultyProfile profile, IReadOnlyList<ExamDifficultyProfileLevel> levels);
}
