using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Bản chụp câu hỏi lúc đưa vào đề, để sửa ngân hàng sau này không đổi đề đã phát.
public class ExamQuestion
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long? SourceQuestionId { get; set; }
    public long? TopicId { get; set; }
    public string ContentSnapshot { get; set; } = string.Empty;
    public QuestionType Type { get; set; }
    public QuestionLevel Level { get; set; }
    public byte? DifficultyLevelId { get; set; }
    public decimal Points { get; set; }
    public int SortOrder { get; set; }

    public Exam Exam { get; set; } = null!;
    public Question? SourceQuestion { get; set; }
    public ICollection<ExamOption> Options { get; set; } = new List<ExamOption>();
}
