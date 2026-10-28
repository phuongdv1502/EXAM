using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

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
