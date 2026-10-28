using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

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
