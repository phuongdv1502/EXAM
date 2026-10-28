namespace AIQuizHSG.Application.Persistence;

// Chỉ đưa dòng nhật ký vào bộ nhớ theo dõi. Lưu cùng transaction với thao tác chính.
public interface IAuditLogRepository
{
    void Add(long? userId, string action, string entityName, long? entityId, string? details, string? ipAddress);
}
