using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

public sealed class QuestionRepository(AiQuizDbContext db) : IQuestionRepository
{
    public Task<Question?> FindAsync(long id, CancellationToken ct) =>
        db.Questions
            .Include(question => question.Options)
            .Include(question => question.DifficultyLevel)
            .Include(question => question.Topic)
            .ThenInclude(topic => topic.Subject)
            .FirstOrDefaultAsync(question => question.Id == id, ct);

    public async Task<(int Total, List<Question> Items)> SearchAsync(QuestionFilter filter, CancellationToken ct)
    {
        var query = ApplyFilter(
        db.Questions.AsNoTracking()
            .Include(question => question.Topic)
            .ThenInclude(topic => topic.Subject)
            .Include(question => question.DifficultyLevel),
            filter);

        var total = await query.CountAsync(ct);
        var items = await query
            .OrderByDescending(question => question.UpdatedAtUtc)
            .ThenByDescending(question => question.Id)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync(ct);
        return (total, items);
    }

    // Giá trị null nghĩa là không lọc theo mục đó. Từ khóa toàn khoảng trắng thì bỏ qua.
    private static IQueryable<Question> ApplyFilter(IQueryable<Question> query, QuestionFilter filter)
    {
        var keyword = string.IsNullOrWhiteSpace(filter.Keyword) ? null : filter.Keyword.Trim();
        return query.Where(question =>
            (filter.SubjectId == null || question.Topic.SubjectId == filter.SubjectId)
            && (filter.TopicId == null || question.TopicId == filter.TopicId)
            && (filter.Level == null || question.Level == filter.Level)
            && (filter.DifficultyLevelId == null || question.DifficultyLevelId == filter.DifficultyLevelId)
            && (filter.Status == null || question.Status == filter.Status)
            && (keyword == null || question.Content.Contains(keyword)));
    }

    // Không theo dõi. Bản chụp đề chỉ đọc nội dung, không sửa câu gốc.
    public Task<List<Question>> ListWithOptionsAsync(IReadOnlyCollection<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
            return Task.FromResult(new List<Question>());

        return db.Questions.AsNoTracking()
            .Include(question => question.Options)
            .Include(question => question.Topic)
            .Where(question => ids.Contains(question.Id))
            .ToListAsync(ct);
    }

    // Chỉ câu đã duyệt, đã có mức 5 bậc, và thuộc môn của đề.
    public Task<List<Question>> ListEligibleAsync(long subjectId, IReadOnlyCollection<long> topicIds, QuestionType? type, CancellationToken ct)
    {
        var query = db.Questions.AsNoTracking()
            .Include(question => question.Options)
            .Include(question => question.Topic)
            .Where(question => question.Status == QuestionStatus.Approved
                && question.DifficultyLevelId != null
                && question.Topic.SubjectId == subjectId);
        if (topicIds.Count > 0)
            query = query.Where(question => topicIds.Contains(question.TopicId));
        if (type is not null)
            query = query.Where(question => question.Type == type);
        return query.ToListAsync(ct);
    }

    public async Task AddAsync(Question question, CancellationToken ct) =>
        await db.Questions.AddAsync(question, ct);

    // Xóa phương án cũ rồi gắn danh sách mới, tránh để đáp án đã bỏ sót lại trong database.
    public void ReplaceOptions(Question question, IReadOnlyList<QuestionOption> options)
    {
        foreach (var option in question.Options.ToList())
            db.QuestionOptions.Remove(option);
        question.Options.Clear();
        foreach (var option in options)
            question.Options.Add(option);
    }
}
