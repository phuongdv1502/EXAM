namespace AIQuizHSG.Domain.Entities;

// Số câu và tỷ lệ của một mức trong profile.
public class ExamDifficultyProfileLevel
{
    public long Id { get; set; }
    public long ProfileId { get; set; }
    public byte DifficultyLevelId { get; set; }
    public int BaseQuestionCount { get; set; }
    public decimal Ratio { get; set; }
    public int SortOrder { get; set; }

    public ExamDifficultyProfile Profile { get; set; } = null!;
    public DifficultyLevel DifficultyLevel { get; set; } = null!;
}
