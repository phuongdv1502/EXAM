using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;

namespace AIQuizHSG.Infrastructure.Persistence;

// Chỉ thêm dòng nhật ký vào context. Unit of Work lưu cùng transaction với thao tác chính.
public sealed class AuditLogRepository(AiQuizDbContext db) : IAuditLogRepository
{
    public void Add(long? userId, string action, string entityName, long? entityId, string? details, string? ipAddress)
    {
        db.AuditLogs.Add(new AuditLog
        {
            UserId = userId,
            Action = action,
            EntityName = entityName,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            OccurredAtUtc = DateTime.UtcNow
        });
    }
}
