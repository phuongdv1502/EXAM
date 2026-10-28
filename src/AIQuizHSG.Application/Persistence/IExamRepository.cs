using AIQuizHSG.Application.Exams;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Đề thi, câu đã chụp và danh sách thí sinh.
public interface IExamRepository
{
    Task<List<ExamSummary>> ListAsync(CancellationToken ct);
    Task<List<ExamSummary>> ListOpenForStudentAsync(long userId, CancellationToken ct);
    Task<Exam?> FindAsync(long id, CancellationToken ct);
    Task AddAsync(Exam exam, CancellationToken ct);
    void ReplaceQuestions(Exam exam, IReadOnlyList<ExamQuestion> questions);
    void ReplaceParticipants(Exam exam, IReadOnlyList<long> userIds);
    Task<Exam?> FindByGenerationKeyAsync(string key, CancellationToken ct);
}
