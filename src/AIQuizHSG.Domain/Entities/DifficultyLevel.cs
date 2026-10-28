namespace AIQuizHSG.Domain.Entities;

// Danh mục 5 mức độ của câu hỏi. Không suy ra từ độ khó mục tiêu của đề.
public class DifficultyLevel
{
    public byte Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
