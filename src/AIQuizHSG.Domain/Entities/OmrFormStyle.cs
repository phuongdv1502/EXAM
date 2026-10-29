namespace AIQuizHSG.Domain.Entities;

// Kiểu phiếu dùng khi sinh mẫu. Mỗi kiểu là một cách xếp ô tô.
public class OmrFormStyle
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
