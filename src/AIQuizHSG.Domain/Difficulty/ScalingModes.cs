using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Domain.Difficulty;

public static class ScalingModes
{
    public const string Proportional = "PROPORTIONAL";
    public const string FixedCounts = "FIXED_COUNTS";

    public static bool TryParse(string? raw, out ScalingMode mode)
    {
        switch (raw?.Trim().Replace("_", "", StringComparison.Ordinal).ToUpperInvariant())
        {
            case "PROPORTIONAL":
                mode = ScalingMode.Proportional;
                return true;
            case "FIXEDCOUNTS":
                mode = ScalingMode.FixedCounts;
                return true;
            default:
                mode = default;
                return false;
        }
    }

    public static string ToCode(ScalingMode mode) =>
        mode == ScalingMode.FixedCounts ? FixedCounts : Proportional;
}
