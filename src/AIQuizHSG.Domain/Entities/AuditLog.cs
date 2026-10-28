namespace AIQuizHSG.Domain.Entities;

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
