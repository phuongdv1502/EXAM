using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Báo cáo phân tích kết quả thi do AI tạo, gắn với một đề hoặc một lượt làm.
public class AiAnalysisReport
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long? AttemptId { get; set; }
    public long RequestedById { get; set; }
    public string? Content { get; set; }
    public AiJobStatus Status { get; set; } = AiJobStatus.Pending;
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public Exam Exam { get; set; } = null!;
    public ExamAttempt? Attempt { get; set; }
    public User RequestedBy { get; set; } = null!;
}
