using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

public sealed class DifficultyLevelRepository(AiQuizDbContext db) : IDifficultyLevelRepository
{
    public Task<bool> AnyAsync(CancellationToken ct) => db.DifficultyLevels.AnyAsync(ct);

    public Task<List<DifficultyLevel>> ListActiveAsync(CancellationToken ct) =>
        db.DifficultyLevels.AsNoTracking()
            .Where(level => level.IsActive)
            .OrderBy(level => level.SortOrder)
            .ThenBy(level => level.Id)
            .ToListAsync(ct);

    public Task<DifficultyLevel?> FindAsync(byte id, CancellationToken ct) =>
        db.DifficultyLevels.FirstOrDefaultAsync(level => level.Id == id, ct);

    public Task<DifficultyLevel?> FindByCodeAsync(string code, CancellationToken ct) =>
        db.DifficultyLevels.FirstOrDefaultAsync(level => level.Code == code, ct);

    public async Task AddRangeAsync(IEnumerable<DifficultyLevel> levels, CancellationToken ct) =>
        await db.DifficultyLevels.AddRangeAsync(levels, ct);
}
