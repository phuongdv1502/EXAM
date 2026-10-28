namespace AIQuizHSG.Domain.Entities;

// Học sinh được phép vào một đề.
public class ExamParticipant
{
    public long Id { get; set; }
    public long ExamId { get; set; }
    public long UserId { get; set; }

    public Exam Exam { get; set; } = null!;
    public User User { get; set; } = null!;
}
