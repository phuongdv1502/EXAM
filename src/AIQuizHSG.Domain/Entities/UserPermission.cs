using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Quyền ngoại lệ của một người, ngoài quyền lấy từ vai trò. Deny bỏ quyền, Allow thêm quyền.
public class UserPermission
{
    public long UserId { get; set; }
    public long PermissionId { get; set; }
    public PermissionEffect Effect { get; set; } = PermissionEffect.Allow;
    public DateTime CreatedAtUtc { get; set; }
    public long? CreatedByUserId { get; set; }

    public User User { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
