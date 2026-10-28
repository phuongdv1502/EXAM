namespace AIQuizHSG.Application.Persistence;

public static class UnitOfWorkAudit
{
    // Lưu dữ liệu nghiệp vụ trước, rồi mới lấy mã tự tăng để ghi nhật ký. Cả hai nằm trong một transaction.
    public static Task SaveWithAuditAsync(
        this IUnitOfWork unitOfWork,
        long? userId,
        string action,
        string entityName,
        Func<long?> entityId,
        string? details,
        string? ipAddress,
        CancellationToken ct) =>
        unitOfWork.ExecuteInTransactionAsync(async innerCt =>
        {
            await unitOfWork.SaveChangesAsync(innerCt);
            unitOfWork.AuditLogs.Add(userId, action, entityName, entityId(), details, ipAddress);
            await unitOfWork.SaveChangesAsync(innerCt);
        }, ct);
}
