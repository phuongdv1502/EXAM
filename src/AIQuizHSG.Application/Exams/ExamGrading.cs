using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Exams;

// Một quy tắc chấm cho phiếu OMR và cho lượt làm trực tuyến sau này.
public static class ExamGrading
{
    public readonly record struct Result(decimal Score, decimal MaxScore, int Correct, int Wrong, int Blank);

    public static Result Score(
        IEnumerable<ExamQuestion> questions,
        Func<long, IReadOnlyCollection<long>> selected)
    {
        decimal score = 0;
        decimal max = 0;
        var correct = 0;
        var wrong = 0;
        var blank = 0;

        foreach (var question in questions)
        {
            max += question.Points;
            var picked = selected(question.Id);
            // Không chọn ô nào là câu bỏ trống, không được điểm.
            if (picked.Count == 0)
            {
                blank++;
                continue;
            }

            var expected = question.Options.Where(option => option.IsCorrect).Select(option => option.Id).ToHashSet();
            if (picked.ToHashSet().SetEquals(expected))
            {
                score += question.Points;
                correct++;
            }
            else
            {
                wrong++;
            }
        }

        return new Result(score, max, correct, wrong, blank);
    }
}
