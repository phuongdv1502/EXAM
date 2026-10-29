namespace AIQuizHSG.Domain.Entities;

// Môn một giáo viên được phép dạy. Một người có thể gắn nhiều môn.
public class TeacherSubject
{
    public long UserId { get; set; }
    public long SubjectId { get; set; }

    public User User { get; set; } = null!;
    public Subject Subject { get; set; } = null!;
}
