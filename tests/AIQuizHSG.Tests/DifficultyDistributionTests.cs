using AIQuizHSG.Domain.Difficulty;

namespace AIQuizHSG.Tests;

// Ma trận Khó gốc và co giãn phần dư lớn nhất.
public class DifficultyDistributionTests
{
    private static readonly int[] Baseline = [5, 10, 18, 12, 5];

    [Fact]
    public void Scale_FiftyQuestions_KeepsBaseline()
    {
        var scaled = Scale(50, proportional: true);
        Assert.Equal(Baseline, scaled);
    }

    [Fact]
    public void Scale_SixtyQuestions_UsesLargestRemainder()
    {
        var scaled = Scale(60, proportional: true);
        Assert.Equal(new[] { 6, 12, 22, 14, 6 }, scaled);
    }

    [Fact]
    public void Scale_NonPositiveTotal_IsRejected()
    {
        Assert.False(DifficultyDistribution.TryScale(Baseline, 0, true, out _));
        Assert.False(DifficultyDistribution.TryScale(Baseline, -1, true, out _));
    }

    [Fact]
    public void CompleteBase_RejectsCountThatDoesNotMatchProfile()
    {
        Assert.False(DifficultyDistribution.IsCompleteBase(50, [5, 10, 18, 12, 4]));
        Assert.True(DifficultyDistribution.IsCompleteBase(50, Baseline));
    }

    [Fact]
    public void Scale_AlwaysSumsToTheRequestedTotal()
    {
        for (var total = 1; total <= 80; total++)
        {
            var scaled = Scale(total, proportional: true);
            Assert.Equal(total, scaled.Sum());
        }
    }

    [Fact]
    public void Scale_EqualRemainders_BreaksTiesByLevelOrder()
    {
        var scaled = Assert.IsType<int[]>(ScaleResult([1, 1, 1], 4, proportional: true));
        Assert.Equal(new[] { 2, 1, 1 }, scaled);
    }

    [Fact]
    public void Scale_FixedCounts_DoesNotResize()
    {
        Assert.False(DifficultyDistribution.TryScale(Baseline, 60, false, out _));
        Assert.Equal(Baseline, Scale(50, proportional: false));
    }

    [Fact]
    public void Shortages_ReportTheGapWithoutLoweringTheLevel()
    {
        var shortages = DifficultyDistribution.Shortages(Baseline, new Dictionary<byte, int>
        {
            [1] = 5,
            [2] = 10,
            [3] = 18,
            [4] = 12,
            [5] = 2
        });

        var shortage = Assert.Single(shortages);
        Assert.Equal((byte)5, shortage.Level);
        Assert.Equal(5, shortage.Required);
        Assert.Equal(2, shortage.Available);
        Assert.Equal(3, shortage.Missing);
    }

    private static int[] Scale(int total, bool proportional) =>
        Assert.IsType<int[]>(ScaleResult(Baseline, total, proportional));

    private static int[] ScaleResult(int[] counts, int total, bool proportional)
    {
        Assert.True(DifficultyDistribution.TryScale(counts, total, proportional, out var result));
        return result;
    }
}
