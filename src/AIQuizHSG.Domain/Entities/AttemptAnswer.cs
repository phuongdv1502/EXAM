using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Câu trả lời của một lượt làm, gắn với câu đã chụp trong đề.
public class AttemptAnswer
{
    public long Id { get; set; }
    public long AttemptId { get; set; }
    public long ExamQuestionId { get; set; }
    public string? AnswerText { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ExamAttempt Attempt { get; set; } = null!;
    public ExamQuestion ExamQuestion { get; set; } = null!;
    public ICollection<AttemptAnswerOption> SelectedOptions { get; set; } = new List<AttemptAnswerOption>();
}
