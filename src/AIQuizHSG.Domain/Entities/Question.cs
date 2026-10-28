using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Câu hỏi trong ngân hàng. Trạng thái đi từ nháp, chờ duyệt, đến đã duyệt hoặc bị từ chối.
public class Question
{
    public long Id { get; set; }
    public long TopicId { get; set; }
    public string Content { get; set; } = string.Empty;
    public QuestionLevel Level { get; set; } = QuestionLevel.Medium;
    public byte? DifficultyLevelId { get; set; }
    public QuestionType Type { get; set; } = QuestionType.SingleChoice;
    public QuestionStatus Status { get; set; } = QuestionStatus.Draft;
    public QuestionSource Source { get; set; } = QuestionSource.Manual;
    public string? Explanation { get; set; }
    public string? RejectionReason { get; set; }
    public long CreatedById { get; set; }
    public long? ReviewedById { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public DateTime? ReviewedAtUtc { get; set; }

    public Topic Topic { get; set; } = null!;
    public DifficultyLevel? DifficultyLevel { get; set; }
    public User CreatedBy { get; set; } = null!;
    public User? ReviewedBy { get; set; }
    public ICollection<QuestionOption> Options { get; set; } = new List<QuestionOption>();
}

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
