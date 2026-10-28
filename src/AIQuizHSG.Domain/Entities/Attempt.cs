using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Một lượt làm bài. DueAtUtc là hạn nộp tính từ lúc bắt đầu cộng thời gian làm bài.
public class ExamAttempt
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long UserId { get; set; }
    public int AttemptNumber { get; set; }
    public DateTime StartedAtUtc { get; set; }
    public DateTime DueAtUtc { get; set; }
    public DateTime? SubmittedAtUtc { get; set; }
    public AttemptStatus Status { get; set; } = AttemptStatus.InProgress;
    public decimal? Score { get; set; }
    public decimal? MaxScore { get; set; }

    public Exam Exam { get; set; } = null!;
    public User User { get; set; } = null!;
    public ICollection<AttemptAnswer> Answers { get; set; } = new List<AttemptAnswer>();
}

// Câu trả lời của một lượt làm, gắn với câu đã chụp trong đề.
public class AttemptAnswer
{
    public long Id { get; set; }
    public long AttemptId { get; set; }
    public long ExamQuestionId { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ExamAttempt Attempt { get; set; } = null!;
    public ExamQuestion ExamQuestion { get; set; } = null!;
    public ICollection<AttemptAnswerOption> SelectedOptions { get; set; } = new List<AttemptAnswerOption>();
}

// Phương án học sinh đã chọn. Phải thuộc đúng câu trong đề.
public class AttemptAnswerOption
{
    public long AttemptAnswerId { get; set; }
    public long ExamQuestionId { get; set; }
    public long ExamOptionId { get; set; }

    public AttemptAnswer AttemptAnswer { get; set; } = null!;
    public ExamOption ExamOption { get; set; } = null!;
}
