using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Entities;

// Ma trận độ khó của cả đề. Sửa bản đã dùng thì tạo bản mới, không đổi đề cũ.
public class ExamDifficultyProfile
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public byte TargetDifficulty { get; set; }
    public int BaseQuestionCount { get; set; }
    public ScalingMode ScalingMode { get; set; } = ScalingMode.Proportional;
    public bool IsSystemDefault { get; set; }
    public int Version { get; set; } = 1;
    public ProfileStatus Status { get; set; } = ProfileStatus.Draft;
    public long CreatedById { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public User CreatedBy { get; set; } = null!;
    public ICollection<ExamDifficultyProfileLevel> Levels { get; set; } = new List<ExamDifficultyProfileLevel>();
}
