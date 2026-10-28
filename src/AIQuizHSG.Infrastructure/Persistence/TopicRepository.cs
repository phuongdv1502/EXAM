using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

public sealed class TopicRepository(AiQuizDbContext db) : ITopicRepository
{
    public Task<List<Topic>> ListAsync(long? subjectId, CancellationToken ct)
    {
        var query = db.Topics.AsNoTracking().AsQueryable();
        if (subjectId is not null)
            query = query.Where(topic => topic.SubjectId == subjectId);
        return query.ToListAsync(ct);
    }

    // Kèm môn học vì câu hỏi và kiểm tra chuyên đề cha cần biết môn.
    public Task<Topic?> FindAsync(long id, CancellationToken ct) =>
        db.Topics.Include(topic => topic.Subject).FirstOrDefaultAsync(topic => topic.Id == id, ct);

    public async Task AddAsync(Topic topic, CancellationToken ct) =>
        await db.Topics.AddAsync(topic, ct);
}
