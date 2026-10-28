using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

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
