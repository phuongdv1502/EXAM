using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Danh mục khối lớp dùng chung.
public interface IGradeLevelRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<List<GradeLevel>> ListAsync(CancellationToken ct);
    Task<GradeLevel?> FindAsync(long id, CancellationToken ct);
    Task<GradeLevel?> FindByCodeAsync(string code, CancellationToken ct);
    Task AddAsync(GradeLevel level, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<GradeLevel> levels, CancellationToken ct);
}
