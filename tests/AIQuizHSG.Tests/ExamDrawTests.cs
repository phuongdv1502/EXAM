using AIQuizHSG.Application.Organization;

namespace AIQuizHSG.Tests;

public class ExamDrawTests
{
    [Fact]
    public void Deal_GivesEachWaitingStudentOneExamAndKeepsTheCountsClose()
    {
        var students = new long[] { 1, 2, 3, 4, 5, 6 };
        var exams = new long[] { 10, 20, 30 };

        var seats = ExamDraw.Deal(students, exams, new Dictionary<long, long>(), new Random(4));

        Assert.Equal(students.OrderBy(id => id), seats.Select(seat => seat.StudentId).OrderBy(id => id));
        Assert.Equal(6, seats.Count);
        var counts = seats.GroupBy(seat => seat.ExamId).ToDictionary(group => group.Key, group => group.Count());
        Assert.Equal(3, counts.Count);
        Assert.All(exams, examId => Assert.Equal(2, counts[examId]));
    }

    [Fact]
    public void Deal_LeavesStudentsWhoAlreadyHaveAnExamInThePool()
    {
        var seated = new Dictionary<long, long> { [1] = 10, [2] = 10 };
        var seats = ExamDraw.Deal([1, 2, 3, 4], [10, 20], seated, new Random(2));

        Assert.DoesNotContain(seats, seat => seat.StudentId is 1 or 2);
        Assert.Equal(2, seats.Count);
        Assert.Contains(seats, seat => seat.ExamId == 20);
    }
}