using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Exams;
using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Difficulty;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Domain.Exams;
using FluentValidation;

namespace AIQuizHSG.Application.Difficulty;

public interface IExamGenerationService
{
    Task<OperationResult<AvailabilityResponse>> CheckAsync(AvailabilityRequest request, CancellationToken ct);
    Task<OperationResult<ExamResponse>> GenerateAsync(GenerateExamRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<ExamValidationResponse>> ValidateAsync(long examId, CancellationToken ct);
}

// Sinh đề theo ma trận. Thiếu câu thì báo số thiếu, không hạ mức.
public sealed partial class ExamGenerationService : TimedService, IExamGenerationService
{
    private readonly IValidator<GenerateExamRequest> generateValidator;
    private readonly IValidator<AvailabilityRequest> availabilityValidator;

    public ExamGenerationService(
        IUnitOfWork unitOfWork,
        IClock clock,
        IValidator<GenerateExamRequest> generateValidator,
        IValidator<AvailabilityRequest> availabilityValidator) : base(unitOfWork, clock)
    {
        this.generateValidator = generateValidator;
        this.availabilityValidator = availabilityValidator;
    }

    public async Task<OperationResult<AvailabilityResponse>> CheckAsync(AvailabilityRequest request, CancellationToken ct)
    {
        request.TopicIds ??= [];
        if (await availabilityValidator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<AvailabilityResponse>.Invalid(inputError);

        var planned = await PlanAsync(request.ProfileId, request.SubjectId, request.QuestionCount, request.TopicIds, request.QuestionType, ct);
        if (planned.Error is not null && planned.Availability is null)
            return OperationResult<AvailabilityResponse>.Invalid(planned.Error);
        return OperationResult<AvailabilityResponse>.Ok(planned.Availability!);
    }

    public async Task<OperationResult<ExamResponse>> GenerateAsync(GenerateExamRequest request, Actor actor, CancellationToken ct)
    {
        request.TopicIds ??= [];
        request.ParticipantUserIds ??= [];
        if (await generateValidator.FirstErrorAsync(request, ct) is { } inputError)
            return OperationResult<ExamResponse>.Invalid(inputError);

        var key = string.IsNullOrWhiteSpace(request.IdempotencyKey) ? null : request.IdempotencyKey.Trim();
        if (key is not null)
        {
            var existing = await unitOfWork.Exams.FindByGenerationKeyAsync(key, ct);
            if (existing is not null)
            {
                // Gửi lại cùng khóa thì trả đề đã sinh, không tạo thêm.
                if (existing.CreatedById != actor.UserId)
                    return OperationResult<ExamResponse>.Invalid(Messages.IdempotencyConflict);
                return OperationResult<ExamResponse>.Ok(ExamMapper.ToResponse(existing));
            }
        }

        var subject = await unitOfWork.Subjects.FindAsync(request.SubjectId, ct);
        if (subject is null || !subject.IsActive)
            return OperationResult<ExamResponse>.Invalid(Messages.SubjectInactive);
        if (await ParticipantErrorAsync(request.ParticipantUserIds, ct) is { } participantError)
            return OperationResult<ExamResponse>.Invalid(participantError);

        var planned = await PlanAsync(request.ProfileId, request.SubjectId, request.QuestionCount, request.TopicIds, request.QuestionType, ct);
        if (planned.Availability is { CanGenerate: false })
            return OperationResult<ExamResponse>.Invalid(planned.Availability.Errors[0].Message);
        if (planned.Error is not null || planned.Selected is null || planned.Profile is null)
            return OperationResult<ExamResponse>.Invalid(planned.Error ?? Messages.ProfileNotFound);

        var seed = Random.Shared.Next(1, int.MaxValue);
        var selected = Pick(planned.Selected, planned.Required, seed);
        var exam = new Exam
        {
            CreatedById = actor.UserId,
            CreatedAtUtc = clock.UtcNow,
            Status = ExamStatus.Draft,
            Subject = subject,
            DifficultyProfileId = planned.Profile.Id,
            DifficultyProfileVersion = planned.Profile.Version,
            TargetDifficultySnapshot = planned.Profile.TargetDifficulty,
            QuestionCountSnapshot = request.QuestionCount,
            GenerationSeed = seed,
            GenerationAlgorithmVersion = DifficultyDistribution.AlgorithmVersion,
            GenerationRequestKey = key
        };
        ExamMapper.Apply(exam, ToSaveRequest(request, selected));
        var order = 1;
        foreach (var question in selected)
            exam.Questions.Add(ExamMapper.Snapshot(question, request.DefaultPoints, order++));
        foreach (var userId in request.ParticipantUserIds.Distinct())
            exam.Participants.Add(new ExamParticipant { UserId = userId });

        await unitOfWork.Exams.AddAsync(exam, ct);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.GenerateExam, AuditCodes.Exam, () => exam.Id, planned.Profile.Id.ToString(), actor.IpAddress, ct);
        var names = await ParticipantNamesAsync(exam, ct);
        return OperationResult<ExamResponse>.Ok(ExamMapper.ToResponse(exam, names));
    }

    public async Task<OperationResult<ExamValidationResponse>> ValidateAsync(long examId, CancellationToken ct)
    {
        var exam = await unitOfWork.Exams.FindAsync(examId, ct);
        if (exam is null)
            return OperationResult<ExamValidationResponse>.NotFound(Messages.ExamNotFound);

        var errors = new List<AvailabilityError>();
        if (exam.Questions.Count == 0)
            errors.Add(new AvailabilityError(DifficultyCodes.InvalidProfile, Messages.ExamHasNoQuestions));

        if (exam.DifficultyProfileId is long profileId)
        {
            var profile = await unitOfWork.ExamProfiles.FindAsync(profileId, ct);
            if (profile is null)
                errors.Add(new AvailabilityError(DifficultyCodes.InvalidProfile, Messages.ProfileNotFound));
            else if (ExamMatrixCheck.Mismatch(profile, exam.Questions, exam.QuestionCountSnapshot) is { } mismatch)
                errors.Add(new AvailabilityError(DifficultyCodes.DistributionMismatch, mismatch));
        }

        return OperationResult<ExamValidationResponse>.Ok(new ExamValidationResponse(errors.Count == 0, errors));
    }

    private async Task<Plan> PlanAsync(
        long profileId,
        long subjectId,
        int questionCount,
        IReadOnlyList<long> topicIds,
        QuestionType? type,
        CancellationToken ct)
    {
        var profile = await unitOfWork.ExamProfiles.FindAsync(profileId, ct);
        if (profile is null || profile.Status == ProfileStatus.Archived)
            return Plan.Fail(Messages.ProfileNotFound);

        var ordered = profile.Levels.OrderBy(level => level.SortOrder).ThenBy(level => level.DifficultyLevelId).ToList();
        var scaled = ExamMatrixCheck.Scale(profile, ordered, questionCount);
        if (scaled.Error is not null)
        {
            return Plan.Blocked(new AvailabilityResponse(
                false,
                questionCount,
                0,
                [],
                [new AvailabilityError(DifficultyCodes.FixedCountMismatch, scaled.Error)]));
        }

        var eligible = await unitOfWork.Questions.ListEligibleAsync(subjectId, topicIds, type, ct);
        var available = eligible
            .GroupBy(question => question.DifficultyLevelId ?? 0)
            .ToDictionary(group => group.Key, group => group.Count());
        var shortages = DifficultyDistribution.Shortages(scaled.Counts, available);
        var response = new AvailabilityResponse(
            shortages.Count == 0,
            questionCount,
            eligible.Count,
            shortages.Select(item => new ShortageLine(item.Level, item.Required, item.Available, item.Missing)).ToArray(),
            shortages.Select(item => new AvailabilityError(
                DifficultyCodes.InsufficientQuestions,
                string.Format(Messages.ShortageMessage, item.Level, item.Missing))).ToArray());
        return new Plan(profile, eligible, scaled.Counts, response, null);
    }

    // Chọn không lặp. Hạt giống giúp cùng một lần sinh ra cùng tập câu.
    private static List<Question> Pick(IReadOnlyList<Question> eligible, int[] required, int seed)
    {
        var selected = new List<Question>();
        for (var index = 0; index < required.Length; index++)
        {
            var level = (byte)(index + 1);
            var pool = eligible.Where(question => question.DifficultyLevelId == level).ToList();
            var ordered = ExamOrder.Arrange(pool, pool.Count > 1, ExamOrder.Seed(seed, level), _ => 0, question => question.Id);
            selected.AddRange(ordered.Take(required[index]));
        }

        return selected;
    }

    private async Task<string?> ParticipantErrorAsync(IReadOnlyList<long> ids, CancellationToken ct)
    {
        if (ids.Count == 0)
            return null;
        if (ids.Distinct().Count() != ids.Count)
            return Messages.ExamParticipantDuplicate;

        var users = await unitOfWork.Users.ListByIdsAsync(ids, ct);
        var allowed = users
            .Where(user => user.Status == UserStatus.Active && user.UserRoles.Any(link => link.Role.Code == RoleCodes.Student))
            .Select(user => user.Id)
            .ToHashSet();
        return ids.All(allowed.Contains) ? null : Messages.ExamParticipantInvalid;
    }

    private async Task<IReadOnlyDictionary<long, User>> ParticipantNamesAsync(Exam exam, CancellationToken ct)
    {
        if (exam.Participants.Count == 0)
            return new Dictionary<long, User>();
        var users = await unitOfWork.Users.ListByIdsAsync(exam.Participants.Select(participant => participant.UserId).ToArray(), ct);
        return users.ToDictionary(user => user.Id);
    }

    private static SaveExamRequest ToSaveRequest(GenerateExamRequest request, IReadOnlyList<Question> selected) => new()
    {
        Title = request.Title,
        Description = request.Description,
        SubjectId = request.SubjectId,
        DurationMinutes = request.DurationMinutes,
        OpensAtUtc = request.OpensAtUtc,
        ClosesAtUtc = request.ClosesAtUtc,
        ShuffleQuestions = request.ShuffleQuestions,
        ShuffleOptions = request.ShuffleOptions,
        MaxAttempts = request.MaxAttempts,
        PublishResults = request.PublishResults,
        Questions = selected.Select(question => new ExamQuestionInput { QuestionId = question.Id, Points = request.DefaultPoints }).ToList(),
        ParticipantUserIds = request.ParticipantUserIds
    };

    private sealed record Plan(
        ExamDifficultyProfile? Profile,
        List<Question>? Selected,
        int[] Required,
        AvailabilityResponse? Availability,
        string? Error)
    {
        public static Plan Fail(string error) => new(null, null, [], null, error);

        public static Plan Blocked(AvailabilityResponse availability) => new(null, null, [], availability, availability.Errors[0].Message);
    }
}
