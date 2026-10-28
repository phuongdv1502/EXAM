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
