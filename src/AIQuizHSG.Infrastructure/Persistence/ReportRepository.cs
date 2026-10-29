using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Reports;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

// Gom số lượng theo nhóm để vẽ biểu đồ, không tải nội dung câu hay bài làm.
public sealed class ReportRepository(AiQuizDbContext db) : IReportRepository
{
    public async Task<DashboardReport> LoadDashboardAsync(CancellationToken ct)
    {
        var questionStatus = await db.Questions.AsNoTracking()
            .GroupBy(question => question.Status)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(ct);
        var difficultyLevels = await db.DifficultyLevels.AsNoTracking()
            .OrderBy(level => level.SortOrder)
            .ToListAsync(ct);
        var difficultyCounts = await db.Questions.AsNoTracking()
            .GroupBy(question => question.DifficultyLevelId)
            .Select(group => new { LevelId = group.Key, Count = group.Count() })
            .ToListAsync(ct);
        var legacyLevels = await db.Questions.AsNoTracking()
            .Where(question => question.DifficultyLevelId == null)
            .GroupBy(question => question.Level)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(ct);
        var bySubject = await db.Questions.AsNoTracking()
            .GroupBy(question => question.Topic.Subject.Name)
            .Select(group => new SubjectCount(group.Key, group.Count()))
            .ToListAsync(ct);
        var examStatus = await db.Exams.AsNoTracking()
            .GroupBy(exam => exam.Status)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(ct);
        var exams = await db.Exams.AsNoTracking()
            .OrderByDescending(exam => exam.Id)
            .Select(exam => new
            {
                exam.Id,
                exam.Title,
                SubjectName = exam.Subject.Name,
                exam.Status,
                QuestionCount = exam.Questions.Count,
            })
            .ToListAsync(ct);
        var attempts = await db.ExamAttempts.AsNoTracking()
            .Select(attempt => new AttemptSlice(attempt.ExamId, attempt.Status, attempt.Score, attempt.MaxScore))
            .ToListAsync(ct);
        var omrStatus = await db.OmrImportBatches.AsNoTracking()
            .GroupBy(batch => batch.Status)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToListAsync(ct);

        var difficulty = difficultyLevels
            .Select(level => new DifficultyCount(
                level.Name,
                level.SortOrder,
                difficultyCounts.FirstOrDefault(row => row.LevelId == level.Id)?.Count ?? 0))
            .ToList();
        // Câu cũ chưa gắn mức 1–5 thì đếm theo mức Dễ/Trung bình/Khó.
        foreach (var row in legacyLevels)
            difficulty.Add(new DifficultyCount(row.Key.ToString(), 100 + (int)row.Key, row.Count));

        var below = 0;
        var from50 = 0;
        var from65 = 0;
        var from80 = 0;
        foreach (var percent in attempts.Select(Percent).Where(value => value is not null))
        {
            if (percent < 50) below++;
            else if (percent < 65) from50++;
            else if (percent < 80) from65++;
            else from80++;
        }

        var lines = exams.Select(exam =>
        {
            var rows = attempts.Where(attempt => attempt.ExamId == exam.Id).ToList();
            var scored = rows.Select(attempt => Percent(attempt)).Where(value => value is not null).Select(value => value!.Value).ToList();
            return new ExamReportLine(
                exam.Id,
                exam.Title,
                exam.SubjectName,
                exam.Status.ToString(),
                exam.QuestionCount,
                rows.Count(attempt => attempt.Status is AttemptStatus.Submitted or AttemptStatus.Graded),
                rows.Count(attempt => attempt.Status == AttemptStatus.InProgress),
                scored.Count == 0 ? null : Math.Round(scored.Average(), 1));
        }).ToList();

        return new DashboardReport(
            questionStatus.Sum(row => row.Count),
            exams.Count,
            attempts.Count,
            await db.Students.CountAsync(ct),
            await db.Subjects.CountAsync(ct),
            await db.OmrScannedSheets.CountAsync(ct),
            await db.OmrScannedSheets.CountAsync(sheet => sheet.Status == OmrSheetStatus.NeedsReview, ct),
            questionStatus.Select(row => new CountItem(row.Key.ToString(), row.Count)).ToList(),
            difficulty.OrderBy(row => row.SortOrder).ToList(),
            bySubject.OrderByDescending(row => row.Count).ThenBy(row => row.Name).ToList(),
            examStatus.Select(row => new CountItem(row.Key.ToString(), row.Count)).ToList(),
            attempts.GroupBy(attempt => attempt.Status)
                .Select(group => new CountItem(group.Key.ToString(), group.Count()))
                .ToList(),
            [
                new CountItem("Below50", below),
                new CountItem("From50", from50),
                new CountItem("From65", from65),
                new CountItem("From80", from80),
            ],
            lines,
            omrStatus.Select(row => new CountItem(row.Key.ToString(), row.Count)).ToList());
    }

    private static decimal? Percent(AttemptSlice attempt) =>
        attempt.Score is null || attempt.MaxScore is null || attempt.MaxScore <= 0
            ? null
            : attempt.Score.Value / attempt.MaxScore.Value * 100m;

    private sealed record AttemptSlice(long ExamId, AttemptStatus Status, decimal? Score, decimal? MaxScore);
}
