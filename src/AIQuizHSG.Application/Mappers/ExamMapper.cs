using AIQuizHSG.Application.Exams;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Domain.Exams;

namespace AIQuizHSG.Application.Mappers;

// Chụp câu hỏi vào đề và dựng đề học sinh xem. Đề học sinh không mang đáp án đúng.
public static class ExamMapper
{
    public static void Apply(Exam exam, SaveExamRequest request)
    {
        exam.Title = request.Title.Trim();
        exam.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        exam.SubjectId = request.SubjectId;
        exam.DurationMinutes = request.DurationMinutes;
        exam.OpensAtUtc = NormalizeUtc(request.OpensAtUtc);
        exam.ClosesAtUtc = NormalizeUtc(request.ClosesAtUtc);
        exam.ShuffleQuestions = request.ShuffleQuestions;
        exam.ShuffleOptions = request.ShuffleOptions;
        exam.MaxAttempts = request.MaxAttempts;
        exam.PublishResults = request.PublishResults;
    }

    // Chỉ chép nội dung và đáp án tại thời điểm đưa vào đề, không gắn navigation câu gốc.
    public static ExamQuestion Snapshot(Question source, decimal points, int sortOrder)
    {
        var examQuestion = new ExamQuestion
        {
            SourceQuestionId = source.Id,
            TopicId = source.TopicId,
            ContentSnapshot = source.Content,
            Type = source.Type,
            Level = source.Level,
            DifficultyLevelId = source.DifficultyLevelId,
            Points = points,
            SortOrder = sortOrder
        };

        var order = 1;
        foreach (var option in source.Options.OrderBy(item => item.SortOrder).ThenBy(item => item.Id))
        {
            examQuestion.Options.Add(new ExamOption
            {
                SourceOptionId = option.Id,
                ContentSnapshot = option.Content,
                IsCorrect = option.IsCorrect,
                SortOrder = order++
            });
        }

        return examQuestion;
    }

    public static ExamResponse ToResponse(Exam exam, IReadOnlyDictionary<long, User>? users = null) => new(
        exam.Id,
        exam.Title,
        exam.Description,
        exam.SubjectId,
        exam.Subject?.Name ?? string.Empty,
        exam.DurationMinutes,
        exam.OpensAtUtc,
        exam.ClosesAtUtc,
        exam.ShuffleQuestions,
        exam.ShuffleOptions,
        exam.MaxAttempts,
        exam.PublishResults,
        exam.Status,
        exam.PublishedAtUtc,
        exam.DifficultyProfileId,
        exam.DifficultyProfileVersion,
        exam.TargetDifficultySnapshot,
        exam.QuestionCountSnapshot,
        exam.Questions
            .OrderBy(question => question.SortOrder)
            .ThenBy(question => question.Id)
            .Select(question => new ExamQuestionResponse(
                question.Id,
                question.SourceQuestionId,
                question.TopicId,
                question.ContentSnapshot,
                question.Type,
                question.Level,
                question.Points,
                question.SortOrder,
                question.DifficultyLevelId,
                question.Options
                    .OrderBy(option => option.SortOrder)
                    .ThenBy(option => option.Id)
                    .Select(option => new ExamOptionResponse(option.Id, option.ContentSnapshot, option.IsCorrect, option.SortOrder))
                    .ToArray()))
            .ToArray(),
        exam.Participants
            .Select(participant => ToParticipant(participant, users))
            .OrderBy(participant => participant.FullName)
            .ThenBy(participant => participant.UserId)
            .ToArray());

    public static AttemptPaper ToPaper(ExamAttempt attempt, DateTime serverNowUtc)
    {
        var exam = attempt.Exam;
        var questions = ExamOrder.Arrange(
            exam.Questions,
            exam.ShuffleQuestions,
            ExamOrder.Seed(attempt.Id),
            question => question.SortOrder,
            question => question.Id);

        return new AttemptPaper(
            attempt.Id,
            attempt.ExamId,
            exam.Title,
            attempt.AttemptNumber,
            attempt.Status,
            attempt.StartedAtUtc,
            attempt.DueAtUtc,
            serverNowUtc,
            questions.Select(question => ToPaperQuestion(attempt, exam.ShuffleOptions, question)).ToArray());
    }

    public static AttemptSummary ToSummary(ExamAttempt attempt) => new(
        attempt.Id,
        attempt.ExamId,
        attempt.Exam?.Title ?? string.Empty,
        attempt.AttemptNumber,
        attempt.Status,
        attempt.StartedAtUtc,
        attempt.DueAtUtc,
        attempt.SubmittedAtUtc);

    public static DateTime? NormalizeUtc(DateTime? value)
    {
        if (value is null)
            return null;

        return value.Value.Kind switch
        {
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            DateTimeKind.Unspecified => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
            _ => value.Value
        };
    }

    private static PaperQuestion ToPaperQuestion(ExamAttempt attempt, bool shuffleOptions, ExamQuestion question)
    {
        var options = ExamOrder.Arrange(
            question.Options,
            shuffleOptions,
            ExamOrder.Seed(attempt.Id, question.Id),
            option => option.SortOrder,
            option => option.Id);
        var selected = attempt.Answers
            .Where(answer => answer.ExamQuestionId == question.Id)
            .SelectMany(answer => answer.SelectedOptions)
            .Select(option => option.ExamOptionId)
            .ToArray();

        return new PaperQuestion(
            question.Id,
            question.ContentSnapshot,
            question.Type,
            question.Level,
            question.Points,
            options.Select(option => new PaperOption(option.Id, option.ContentSnapshot)).ToArray(),
            selected);
    }

    private static ExamParticipantResponse ToParticipant(ExamParticipant participant, IReadOnlyDictionary<long, User>? users)
    {
        var user = participant.User;
        if (user is null && users is not null)
            users.TryGetValue(participant.UserId, out user);

        return new ExamParticipantResponse(participant.UserId, user?.FullName ?? string.Empty, user?.Username ?? string.Empty);
    }
}
