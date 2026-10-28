using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Chuyên đề, có thể lọc theo môn và đi lên chuyên đề cha.
public interface ITopicRepository
{
    Task<List<Topic>> ListAsync(long? subjectId, CancellationToken ct);
    Task<Topic?> FindAsync(long id, CancellationToken ct);
    Task AddAsync(Topic topic, CancellationToken ct);
}
