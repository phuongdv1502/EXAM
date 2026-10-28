using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

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
