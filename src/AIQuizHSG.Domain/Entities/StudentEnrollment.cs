using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Lịch sử phân lớp. Chuyển lớp thì đóng bản ghi cũ, không ghi đè.
public class StudentEnrollment
{
    public long Id { get; set; }
    public long StudentId { get; set; }
    public long ClassroomId { get; set; }
    public DateOnly EnrollmentDate { get; set; }
    public DateOnly? LeaveDate { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    // Số báo danh trong lớp, dùng tô trên phiếu OMR. Khác mã học sinh hệ thống.
    public string? SeatCode { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public StudentProfile Student { get; set; } = null!;
    public Classroom Classroom { get; set; } = null!;
}
