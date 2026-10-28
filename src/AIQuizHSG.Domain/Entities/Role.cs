namespace AIQuizHSG.Domain.Entities;

// Vai trò ADMIN, TEACHER, STUDENT hoặc SCHOOL_ADMIN.
public class Role
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}
