namespace AIQuizHSG.Domain.Entities;

// Môn học. Mã là duy nhất, ví dụ TIN.
public class Subject
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Topic> Topics { get; set; } = new List<Topic>();
}
