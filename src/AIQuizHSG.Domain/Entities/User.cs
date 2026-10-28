using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Tài khoản đăng nhập. Mật khẩu chỉ lưu dạng đã băm.
public class User
{
    public long Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public UserStatus Status { get; set; } = UserStatus.Active;
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

// Vai trò ADMIN, TEACHER hoặc STUDENT.
public class Role
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}

// Một tài khoản có thể gắn nhiều vai trò.
public class UserRole
{
    public long UserId { get; set; }
    public long RoleId { get; set; }

    public User User { get; set; } = null!;
    public Role Role { get; set; } = null!;
}
