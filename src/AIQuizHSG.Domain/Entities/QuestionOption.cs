namespace AIQuizHSG.Domain.Entities;

// Một phương án của câu hỏi. SortOrder là thứ tự hiển thị.
public class QuestionOption
{
    public long Id { get; set; }
    public long QuestionId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCorrect { get; set; }
    public int SortOrder { get; set; }

    public Question Question { get; set; } = null!;
}
