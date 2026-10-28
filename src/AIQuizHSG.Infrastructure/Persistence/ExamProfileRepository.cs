using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

public sealed class ExamProfileRepository(AiQuizDbContext db) : IExamProfileRepository
{
    public Task<bool> AnySystemDefaultAsync(CancellationToken ct) =>
        db.ExamDifficultyProfiles.AnyAsync(profile => profile.IsSystemDefault, ct);

    public Task<List<ExamDifficultyProfile>> ListAsync(CancellationToken ct) =>
        Query().OrderBy(profile => profile.TargetDifficulty).ThenBy(profile => profile.Name).ToListAsync(ct);

    public Task<ExamDifficultyProfile?> FindAsync(long id, CancellationToken ct) =>
        Query().FirstOrDefaultAsync(profile => profile.Id == id, ct);

    public Task<bool> IsUsedAsync(long id, CancellationToken ct) =>
        db.Exams.AnyAsync(exam => exam.DifficultyProfileId == id, ct);

    public async Task AddAsync(ExamDifficultyProfile profile, CancellationToken ct) =>
        await db.ExamDifficultyProfiles.AddAsync(profile, ct);

    public void ReplaceLevels(ExamDifficultyProfile profile, IReadOnlyList<ExamDifficultyProfileLevel> levels)
    {
        db.ExamDifficultyProfileLevels.RemoveRange(profile.Levels);
        profile.Levels.Clear();
        foreach (var level in levels)
            profile.Levels.Add(level);
    }

    private IQueryable<ExamDifficultyProfile> Query() =>
        db.ExamDifficultyProfiles
            .Include(profile => profile.Levels)
            .ThenInclude(level => level.DifficultyLevel);
}
