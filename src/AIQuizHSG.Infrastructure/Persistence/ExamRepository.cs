using AIQuizHSG.Application.Exams;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

public sealed class ExamRepository(AiQuizDbContext db) : IExamRepository
{
    public async Task<List<ExamSummary>> ListAsync(CancellationToken ct)
    {
        var rows = await Project(db.Exams.AsNoTracking()
                .OrderByDescending(exam => exam.CreatedAtUtc)
                .ThenByDescending(exam => exam.Id))
            .ToListAsync(ct);
        return rows.Select(ToSummary).ToList();
    }

    public async Task<List<ExamSummary>> ListOpenForStudentAsync(long userId, CancellationToken ct)
    {
        // Học sinh chỉ thấy đề được chỉ đích danh, hoặc đề giao cho lớp mình đang học.
        var studentIds = await db.Students.AsNoTracking()
            .Where(student => student.UserId == userId)
            .Select(student => student.Id)
            .ToListAsync(ct);
        var classIds = await db.StudentEnrollments.AsNoTracking()
            .Where(item => studentIds.Contains(item.StudentId)
                && item.Status == EnrollmentStatus.Active
                && item.LeaveDate == null)
            .Select(item => item.ClassroomId)
            .ToListAsync(ct);
        var assignedExamIds = await db.ExamAssignments.AsNoTracking()
            .Where(item => item.Status == AssignmentStatus.Active
                && ((item.StudentId != null && studentIds.Contains(item.StudentId.Value))
                    || (item.ClassroomId != null && classIds.Contains(item.ClassroomId.Value))))
            .Select(item => item.ExamId)
            .Distinct()
            .ToListAsync(ct);

        var rows = await Project(db.Exams.AsNoTracking()
                .Where(exam => exam.Status == ExamStatus.Published
                    && (exam.Participants.Any(participant => participant.UserId == userId)
                        || (!exam.Participants.Any() && assignedExamIds.Contains(exam.Id))))
                .OrderBy(exam => exam.OpensAtUtc)
                .ThenBy(exam => exam.Id))
            .ToListAsync(ct);
        return rows.Select(ToSummary).ToList();
    }

    public Task<Exam?> FindAsync(long id, CancellationToken ct) =>
        db.Exams
            .Include(exam => exam.Subject)
            .Include(exam => exam.Questions)
            .ThenInclude(question => question.Options)
            .Include(exam => exam.Participants)
            .ThenInclude(participant => participant.User)
            .FirstOrDefaultAsync(exam => exam.Id == id, ct);

    public async Task AddAsync(Exam exam, CancellationToken ct) =>
        await db.Exams.AddAsync(exam, ct);

    public void ReplaceQuestions(Exam exam, IReadOnlyList<ExamQuestion> questions)
    {
        db.ExamQuestions.RemoveRange(exam.Questions);
        exam.Questions.Clear();
        foreach (var question in questions)
            exam.Questions.Add(question);
    }

    public void ReplaceParticipants(Exam exam, IReadOnlyList<long> userIds)
    {
        db.ExamParticipants.RemoveRange(exam.Participants);
        exam.Participants.Clear();
        foreach (var userId in userIds)
            exam.Participants.Add(new ExamParticipant { UserId = userId });
    }

    public Task<Exam?> FindByGenerationKeyAsync(string key, CancellationToken ct) =>
        db.Exams.AsNoTracking()
            .Include(exam => exam.Subject)
            .Include(exam => exam.Questions)
            .ThenInclude(question => question.Options)
            .Include(exam => exam.Participants)
            .ThenInclude(participant => participant.User)
            .FirstOrDefaultAsync(exam => exam.GenerationRequestKey == key, ct);

    private static IQueryable<SummaryRow> Project(IQueryable<Exam> exams) =>
        exams.Select(exam => new SummaryRow(
            exam.Id,
            exam.Title,
            exam.SubjectId,
            exam.Subject.Name,
            exam.DurationMinutes,
            exam.Status,
            exam.Questions.Count,
            exam.Participants.Count,
            exam.PublishResults,
            exam.OpensAtUtc,
            exam.ClosesAtUtc));

    private static ExamSummary ToSummary(SummaryRow exam) => new(
        exam.Id,
        exam.Title,
        exam.SubjectId,
        exam.SubjectName,
        exam.DurationMinutes,
        exam.Status,
        exam.QuestionCount,
        exam.ParticipantCount,
        exam.PublishResults,
        exam.OpensAtUtc,
        exam.ClosesAtUtc);

    private sealed record SummaryRow(
        long Id,
        string Title,
        long SubjectId,
        string SubjectName,
        int DurationMinutes,
        ExamStatus Status,
        int QuestionCount,
        int ParticipantCount,
        bool PublishResults,
        DateTime? OpensAtUtc,
        DateTime? ClosesAtUtc);
}
