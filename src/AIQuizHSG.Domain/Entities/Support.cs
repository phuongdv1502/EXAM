using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Yêu cầu nhờ AI sinh câu. Chưa gọi model, chỉ giữ chỗ cho việc đó.
public class AiQuestionRequest
{
    public long Id { get; set; }
    public long RequestedById { get; set; }
    public long TopicId { get; set; }
    public QuestionLevel Level { get; set; }
    public QuestionType Type { get; set; }
    public string? SourceMaterial { get; set; }
    public int QuestionCount { get; set; }
    public AiJobStatus Status { get; set; } = AiJobStatus.Pending;
    public string? ResultJson { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }

    public User RequestedBy { get; set; } = null!;
    public Topic Topic { get; set; } = null!;
}

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

// Nhật ký thao tác: ai làm gì, trên bản ghi nào, từ địa chỉ IP nào.
public class AuditLog
{
    public long Id { get; set; }
    public long? UserId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityName { get; set; } = string.Empty;
    public long? EntityId { get; set; }
    public string? Details { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAtUtc { get; set; }

    public User? User { get; set; }
}
