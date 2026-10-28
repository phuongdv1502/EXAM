using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Câu hỏi và phương án. Thay phương án thì xóa bản cũ trước khi gắn bản mới.
public interface IQuestionRepository
{
    Task<Question?> FindAsync(long id, CancellationToken ct);
    Task<(int Total, List<Question> Items)> SearchAsync(QuestionFilter filter, CancellationToken ct);
    Task<List<Question>> ListWithOptionsAsync(IReadOnlyCollection<long> ids, CancellationToken ct);
    Task<List<Question>> ListEligibleAsync(long subjectId, IReadOnlyCollection<long> topicIds, QuestionType? type, CancellationToken ct);
    Task AddAsync(Question question, CancellationToken ct);
    void ReplaceOptions(Question question, IReadOnlyList<QuestionOption> options);
}
