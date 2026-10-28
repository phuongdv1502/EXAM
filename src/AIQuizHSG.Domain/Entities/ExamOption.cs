namespace AIQuizHSG.Domain.Entities;

// Bản chụp phương án trong đề. IsCorrect chỉ dùng khi chấm ở máy chủ.
public class ExamOption
{
    public long Id { get; set; }
    public long ExamQuestionId { get; set; }
    public long? SourceOptionId { get; set; }
    public string ContentSnapshot { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }

    public ExamQuestion ExamQuestion { get; set; } = null!;
    public QuestionOption? SourceOption { get; set; }
}
