namespace AIQuizHSG.Domain.Exams;

// Trộn ổn định theo mã lượt thi, để học sinh tải lại đề vẫn thấy cùng thứ tự.
public static class ExamOrder
{
    public static int Seed(long first, long second = 0)
    {
        unchecked
        {
            var hash = 17;
            hash = hash * 31 + (int)first;
            hash = hash * 31 + (int)(first >> 32);
            hash = hash * 31 + (int)second;
            hash = hash * 31 + (int)(second >> 32);
            return hash == 0 ? 1 : hash;
        }
    }

    public static List<T> Arrange<T>(
        IEnumerable<T> items,
        bool shuffle,
        int seed,
        Func<T, int> sortOrder,
        Func<T, long> id)
    {
        var list = items.OrderBy(sortOrder).ThenBy(id).ToList();
        if (!shuffle || list.Count < 2)
            return list;

        var random = new Random(seed);
        for (var index = list.Count - 1; index > 0; index--)
        {
            var swap = random.Next(index + 1);
            (list[index], list[swap]) = (list[swap], list[index]);
        }

        return list;
    }
}
