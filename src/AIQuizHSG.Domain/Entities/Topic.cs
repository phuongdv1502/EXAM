namespace AIQuizHSG.Domain.Entities;

// Chuyên đề thuộc một môn, có thể nằm dưới một chuyên đề cha.
public class Topic
{
    public long Id { get; set; }
    public long SubjectId { get; set; }
    public long? ParentTopicId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;

    public Subject Subject { get; set; } = null!;
    public Topic? Parent { get; set; }
    public ICollection<Topic> Children { get; set; } = new List<Topic>();
    public ICollection<Question> Questions { get; set; } = new List<Question>();
}
