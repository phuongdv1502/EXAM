using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Application.Omr;

// Chọn ô tô bằng độ đậm tương đối trong cùng một câu, không chỉ một ngưỡng cố định.
public static class OmrMarkChoice
{
    public readonly record struct Decision(OmrMarkStatus Status, int? Index);

    public static Decision Choose(IReadOnlyList<double> ratios, double marked = 0.45, double blank = 0.18)
    {
        if (ratios.Count == 0)
            return new Decision(OmrMarkStatus.Blank, null);

        var topIndex = 0;
        var top = ratios[0];
        var next = 0d;
        var strong = top >= marked ? 1 : 0;
        for (var index = 1; index < ratios.Count; index++)
        {
            var ratio = ratios[index];
            if (ratio > top)
            {
                next = top;
                top = ratio;
                topIndex = index;
            }
            else if (ratio > next)
            {
                next = ratio;
            }

            if (ratio >= marked)
                strong++;
        }

        var gap = top - next;
        // Hai ô cùng đậm thì không chọn bừa.
        if (strong > 1)
            return new Decision(OmrMarkStatus.Ambiguous, null);
        // Một ô vượt ngưỡng và đậm hơn các ô còn lại thì nhận, dù vệt mờ kéo ô thứ hai lên.
        if (strong == 1 && gap >= 0.15)
            return new Decision(OmrMarkStatus.Marked, topIndex);
        // Bút chì nhạt chưa tới ngưỡng tuyệt đối, nhưng vẫn trội hơn hẳn ô trống.
        if (strong == 0 && top >= Math.Max(blank, 0.30) && gap >= 0.20)
            return new Decision(OmrMarkStatus.Marked, topIndex);
        if (strong == 1)
            return new Decision(OmrMarkStatus.Ambiguous, null);
        return new Decision(OmrMarkStatus.Blank, null);
    }
}
