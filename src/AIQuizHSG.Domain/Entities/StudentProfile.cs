using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Hồ sơ học sinh, tách khỏi tài khoản đăng nhập. Mã duy nhất trong từng trường.
public class StudentProfile
{
    public long Id { get; set; }
    public long SchoolId { get; set; }
    public string StudentCode { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public DateOnly? DateOfBirth { get; set; }
    public Gender? Gender { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public long? UserId { get; set; }
    public StudentProfileStatus Status { get; set; } = StudentProfileStatus.Active;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public School School { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<StudentEnrollment> Enrollments { get; set; } = new List<StudentEnrollment>();
}
