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
