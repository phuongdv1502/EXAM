namespace AIQuizHSG.Application.Organization;

public readonly record struct ExamSeat(long StudentId, long ExamId);

// Chia các đề cho học sinh chưa có chỗ, mỗi đề được dùng gần bằng nhau.
public static class ExamDraw
{
    public static List<ExamSeat> Deal(
        IReadOnlyList<long> studentIds,
        IReadOnlyList<long> examIds,
        IReadOnlyDictionary<long, long> seated,
        Random random)
    {
        var counts = new Dictionary<long, int>();
        foreach (var examId in examIds.Distinct())
            counts[examId] = 0;
        foreach (var examId in seated.Values)
        {
            if (counts.ContainsKey(examId))
                counts[examId]++;
        }

        var waiting = studentIds.Where(id => !seated.ContainsKey(id)).ToList();
        for (var index = waiting.Count - 1; index > 0; index--)
        {
            var swap = random.Next(index + 1);
            (waiting[index], waiting[swap]) = (waiting[swap], waiting[index]);
        }

        var seats = new List<ExamSeat>(waiting.Count);
        foreach (var studentId in waiting)
        {
            var least = counts.Values.Min();
            var choices = counts.Where(pair => pair.Value == least).Select(pair => pair.Key).ToList();
            var examId = choices[random.Next(choices.Count)];
            counts[examId]++;
            seats.Add(new ExamSeat(studentId, examId));
        }

        return seats;
    }
}
