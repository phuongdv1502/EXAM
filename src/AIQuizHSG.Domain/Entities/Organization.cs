using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Trường học. Một hệ thống có thể có nhiều trường.
public class School
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Address { get; set; }
    public string? ProvinceCode { get; set; }
    public string? DistrictCode { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

// Năm học thuộc một trường. Chỉ một năm được đánh dấu hiện hành.
public class SchoolYear
{
    public long Id { get; set; }
    public long SchoolId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public bool IsCurrent { get; set; }
    public SchoolYearStatus Status { get; set; } = SchoolYearStatus.Planned;

    public School School { get; set; } = null!;
}

// Danh mục khối dùng chung, ví dụ khối 6 đến 9.
public class GradeLevel
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string EducationLevel { get; set; } = "THCS";
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}

// Lớp cụ thể trong một năm học. Giáo viên chủ nhiệm lưu ngay trên lớp.
public class Classroom
{
    public long Id { get; set; }
    public long SchoolId { get; set; }
    public long SchoolYearId { get; set; }
    public long GradeLevelId { get; set; }
    public string ClassCode { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public long? HomeroomTeacherUserId { get; set; }
    public int? Capacity { get; set; }
    public ClassroomStatus Status { get; set; } = ClassroomStatus.Active;

    public School School { get; set; } = null!;
    public SchoolYear SchoolYear { get; set; } = null!;
    public GradeLevel GradeLevel { get; set; } = null!;
    public User? HomeroomTeacher { get; set; }
}

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

// Lịch sử phân lớp. Chuyển lớp thì đóng bản ghi cũ, không ghi đè.
public class StudentEnrollment
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public long ClassroomId { get; set; }
    public DateOnly EnrollmentDate { get; set; }
    public DateOnly? LeaveDate { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public StudentProfile Student { get; set; } = null!;
    public Classroom Classroom { get; set; } = null!;
}

// Người dùng thuộc trường và vai trò tại trường đó.
public class SchoolMembership
{
    public long Id { get; set; }
    public long SchoolId { get; set; }
    public long UserId { get; set; }
    public SchoolRole SchoolRole { get; set; }
    public MembershipStatus Status { get; set; } = MembershipStatus.Active;
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    public School School { get; set; } = null!;
    public User User { get; set; } = null!;
}

// Giáo viên bộ môn của một lớp. Chủ nhiệm không lưu thêm ở đây.
public class ClassMembership
{
    public long Id { get; set; }
    public long ClassroomId { get; set; }
    public long UserId { get; set; }
    public ClassRole ClassRole { get; set; } = ClassRole.SubjectTeacher;
    public long SubjectId { get; set; }

    public Classroom Classroom { get; set; } = null!;
    public User User { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}

// Giao một đề dùng chung cho cả lớp hoặc một học sinh.
public class ExamAssignment
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long? ClassroomId { get; set; }
    public long? StudentId { get; set; }
    public long AssignedByUserId { get; set; }
    public DateTime AssignedAtUtc { get; set; }
    public DateTime? DueAtUtc { get; set; }
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;

    public Exam Exam { get; set; } = null!;
    public Classroom? Classroom { get; set; }
    public StudentProfile? Student { get; set; }
}

// Quyền thao tác, tách khỏi phạm vi trường và lớp.
public class Permission
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Module { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystem { get; set; } = true;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
}

// Quyền được cấp cho một vai trò. Không dùng bảng quyền riêng từng người.
public class RolePermission
{
    public long RoleId { get; set; }
    public long PermissionId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long? CreatedByUserId { get; set; }

    public Role Role { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
