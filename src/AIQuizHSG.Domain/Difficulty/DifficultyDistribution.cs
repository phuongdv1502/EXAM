namespace AIQuizHSG.Domain.Difficulty;

// Co giãn ma trận bằng phần dư lớn nhất. Hòa phần dư thì ưu tiên mức có thứ tự nhỏ hơn.
public static class DifficultyDistribution
{
    public const string AlgorithmVersion = "largest-remainder-v1";
    public const int LevelCount = 5;

    public static bool TryScale(IReadOnlyList<int> baseCounts, int targetTotal, bool proportional, out int[] result)
    {
        result = [];
        if (baseCounts.Count == 0 || targetTotal <= 0 || baseCounts.Any(count => count < 0))
            return false;

        var baseTotal = baseCounts.Sum();
        if (baseTotal <= 0)
            return false;

        // FIXED_COUNTS giữ đúng số câu gốc, không tự đổi cơ cấu.
        if (!proportional)
        {
            if (targetTotal != baseTotal)
                return false;
            result = baseCounts.ToArray();
            return true;
        }

        var scaled = new int[baseCounts.Count];
        var remainders = new decimal[baseCounts.Count];
        var assigned = 0;
        for (var index = 0; index < baseCounts.Count; index++)
        {
            var exact = (decimal)targetTotal * baseCounts[index] / baseTotal;
            var floor = (int)decimal.Floor(exact);
            scaled[index] = floor;
            remainders[index] = exact - floor;
            assigned += floor;
        }

        var order = Enumerable.Range(0, baseCounts.Count)
            .OrderByDescending(index => remainders[index])
            .ThenBy(index => index)
            .ToArray();
        var remaining = targetTotal - assigned;
        for (var extra = 0; extra < remaining; extra++)
            scaled[order[extra]]++;

        result = scaled;
        return scaled.Sum() == targetTotal;
    }

    public static bool IsCompleteBase(int baseQuestionCount, IReadOnlyList<int> counts) =>
        baseQuestionCount > 0
        && counts.Count == LevelCount
        && counts.All(count => count >= 0)
        && counts.Sum() == baseQuestionCount;

    public static List<LevelShortage> Shortages(IReadOnlyList<int> required, IReadOnlyDictionary<byte, int> available)
    {
        var shortages = new List<LevelShortage>();
        for (var index = 0; index < required.Count; index++)
        {
            var level = (byte)(index + 1);
            var have = available.GetValueOrDefault(level);
            if (have >= required[index])
                continue;
            shortages.Add(new LevelShortage(level, required[index], have, required[index] - have));
        }

        return shortages;
    }
}

public readonly record struct LevelShortage(byte Level, int Required, int Available, int Missing);
