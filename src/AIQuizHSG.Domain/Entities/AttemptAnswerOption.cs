namespace AIQuizHSG.Domain.Entities;

// Phương án học sinh đã chọn. Phải thuộc đúng câu trong đề.
public class AttemptAnswerOption
{
    public long AttemptAnswerId { get; set; }
    public long ExamQuestionId { get; set; }
    public long ExamOptionId { get; set; }

    public AttemptAnswer AttemptAnswer { get; set; } = null!;
    public ExamOption ExamOption { get; set; } = null!;
}
