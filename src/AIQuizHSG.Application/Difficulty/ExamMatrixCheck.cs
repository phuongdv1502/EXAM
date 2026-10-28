using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Difficulty;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Difficulty;

// So khớp số câu từng mức của đề với ma trận đã co giãn.
public static class ExamMatrixCheck
{
    public static string? Mismatch(ExamDifficultyProfile profile, IEnumerable<ExamQuestion> questions, int? questionCountSnapshot)
    {
        var ordered = profile.Levels.OrderBy(level => level.SortOrder).ThenBy(level => level.DifficultyLevelId).ToList();
        if (ordered.Count != DifficultyDistribution.LevelCount)
            return Messages.ProfileDistributionInvalid;

        var target = questionCountSnapshot ?? ordered.Sum(level => level.BaseQuestionCount);
        var scaled = Scale(profile, ordered, target);
        if (scaled.Error is not null)
            return scaled.Error;

        var actual = questions
            .GroupBy(question => question.DifficultyLevelId ?? 0)
            .ToDictionary(group => group.Key, group => group.Count());
        for (var index = 0; index < scaled.Counts.Length; index++)
        {
            var level = (byte)(index + 1);
            actual.TryGetValue(level, out var count);
            if (count != scaled.Counts[index])
                return Messages.ExamDistributionInvalid;
        }

        var extra = actual.Where(pair => pair.Key is < 1 or > 5 && pair.Value > 0).Any();
        return extra ? Messages.ExamDistributionInvalid : null;
    }

    public static (int[] Counts, string? Error) Scale(
        ExamDifficultyProfile profile,
        IReadOnlyList<ExamDifficultyProfileLevel> ordered,
        int target)
    {
        var baseCounts = ordered.Select(level => level.BaseQuestionCount).ToArray();
        var proportional = profile.ScalingMode == ScalingMode.Proportional;
        if (!DifficultyDistribution.TryScale(baseCounts, target, proportional, out var counts))
        {
            var message = proportional
                ? Messages.GenerateQuestionCountInvalid
                : string.Format(Messages.ProfileFixedCount, profile.BaseQuestionCount);
            return ([], message);
        }

        return (counts, null);
    }
}
