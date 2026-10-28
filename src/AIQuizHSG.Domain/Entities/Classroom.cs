using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

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
