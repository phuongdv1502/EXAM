namespace AIQuizHSG.Domain.Entities;

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
