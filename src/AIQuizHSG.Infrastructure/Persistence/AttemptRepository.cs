using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

public sealed class AttemptRepository(AiQuizDbContext db) : IAttemptRepository
{
    public Task<int> CountAsync(long examId, long userId, CancellationToken ct) =>
        db.ExamAttempts.CountAsync(attempt => attempt.ExamId == examId && attempt.UserId == userId, ct);

    public Task<ExamAttempt?> FindInProgressAsync(long examId, long userId, CancellationToken ct) =>
        Detail().FirstOrDefaultAsync(attempt =>
            attempt.ExamId == examId
            && attempt.UserId == userId
            && attempt.Status == AttemptStatus.InProgress, ct);

    public Task<ExamAttempt?> FindAsync(long id, CancellationToken ct) =>
        Detail().FirstOrDefaultAsync(attempt => attempt.Id == id, ct);

    public Task<ExamAttempt?> FindLatestAsync(long examId, long userId, CancellationToken ct) =>
        Detail()
            .Where(attempt => attempt.ExamId == examId && attempt.UserId == userId)
            .OrderByDescending(attempt => attempt.AttemptNumber)
            .FirstOrDefaultAsync(ct);

    public Task<List<ExamAttempt>> ListByUserAsync(long userId, CancellationToken ct) =>
        db.ExamAttempts.AsNoTracking()
            .Include(attempt => attempt.Exam)
            .Where(attempt => attempt.UserId == userId)
            .OrderByDescending(attempt => attempt.StartedAtUtc)
            .ThenByDescending(attempt => attempt.Id)
            .ToListAsync(ct);

    public Task<List<ExamAttempt>> ListByUserIdsAsync(IReadOnlyCollection<long> userIds, CancellationToken ct)
    {
        if (userIds.Count == 0)
            return Task.FromResult(new List<ExamAttempt>());

        return db.ExamAttempts.AsNoTracking()
            .Include(attempt => attempt.Exam)
            .Where(attempt => userIds.Contains(attempt.UserId))
            .ToListAsync(ct);
    }

    public async Task AddAsync(ExamAttempt attempt, CancellationToken ct) =>
        await db.ExamAttempts.AddAsync(attempt, ct);

    public void ReplaceSelections(AttemptAnswer answer, IReadOnlyList<long> optionIds)
    {
        db.AttemptAnswerOptions.RemoveRange(answer.SelectedOptions);
        answer.SelectedOptions.Clear();
        foreach (var optionId in optionIds)
        {
            answer.SelectedOptions.Add(new AttemptAnswerOption
            {
                ExamQuestionId = answer.ExamQuestionId,
                ExamOptionId = optionId
            });
        }
    }

    private IQueryable<ExamAttempt> Detail() =>
        db.ExamAttempts
            .Include(attempt => attempt.Exam)
            .ThenInclude(exam => exam.Questions)
            .ThenInclude(question => question.Options)
            .Include(attempt => attempt.Answers)
            .ThenInclude(answer => answer.SelectedOptions);
}
