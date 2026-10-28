using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Năm mức độ câu hỏi. Không tự gán cho câu cũ.
public interface IDifficultyLevelRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<List<DifficultyLevel>> ListActiveAsync(CancellationToken ct);
    Task<DifficultyLevel?> FindAsync(byte id, CancellationToken ct);
    Task<DifficultyLevel?> FindByCodeAsync(string code, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<DifficultyLevel> levels, CancellationToken ct);
}
