using AIQuizHSG.Application.Difficulty;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Difficulty;
using FluentValidation;

namespace AIQuizHSG.Application.Validation;

public sealed class SaveExamProfileRequestValidator : AbstractValidator<SaveExamProfileRequest>
{
    public SaveExamProfileRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(Messages.ProfileNameRequired)
            .Must(name => name.Trim().Length <= 200).WithMessage(Messages.ProfileNameTooLong);

        RuleFor(x => x.TargetDifficulty)
            .InclusiveBetween((byte)1, (byte)5).WithMessage(Messages.TargetDifficultyInvalid);

        RuleFor(x => x.BaseQuestionCount)
            .GreaterThan(0).WithMessage(Messages.ProfileQuestionCountInvalid);

        RuleFor(x => x.ScalingMode)
            .Must(mode => ScalingModes.TryParse(mode, out _)).WithMessage(Messages.ScalingModeInvalid);

        RuleFor(x => x.DifficultyDistribution)
            .NotNull().WithMessage(Messages.ProfileLevelInvalid)
            .Must(items => items is { Count: DifficultyDistribution.LevelCount }).WithMessage(Messages.ProfileLevelInvalid);
    }
}

public sealed class AvailabilityRequestValidator : AbstractValidator<AvailabilityRequest>
{
    public AvailabilityRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.SubjectId).GreaterThan(0).WithMessage(Messages.SubjectNotSelected);
        RuleFor(x => x.ProfileId).GreaterThan(0).WithMessage(Messages.ProfileNotFound);
        RuleFor(x => x.QuestionCount).GreaterThan(0).WithMessage(Messages.GenerateQuestionCountInvalid);
    }
}

public sealed class GenerateExamRequestValidator : AbstractValidator<GenerateExamRequest>
{
    public GenerateExamRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(Messages.ExamTitleRequired)
            .Must(title => title.Trim().Length <= 300).WithMessage(Messages.ExamTitleTooLong);

        RuleFor(x => x.Description)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 5000)
            .WithMessage(Messages.ExamDescriptionTooLong);

        RuleFor(x => x.SubjectId).GreaterThan(0).WithMessage(Messages.SubjectNotSelected);
        RuleFor(x => x.ProfileId).GreaterThan(0).WithMessage(Messages.ProfileNotFound);
        RuleFor(x => x.QuestionCount).GreaterThan(0).WithMessage(Messages.GenerateQuestionCountInvalid);
        RuleFor(x => x.DefaultPoints).GreaterThan(0).LessThanOrEqualTo(100).WithMessage(Messages.ExamPointsInvalid);
        RuleFor(x => x.DurationMinutes).InclusiveBetween(1, 300).WithMessage(Messages.ExamDurationInvalid);
        RuleFor(x => x.MaxAttempts).InclusiveBetween(1, 10).WithMessage(Messages.ExamAttemptsInvalid);
        RuleFor(x => x.ClosesAtUtc)
            .Must((request, closes) => closes is null || request.OpensAtUtc is null || closes > request.OpensAtUtc)
            .WithMessage(Messages.ExamWindowInvalid);
        RuleFor(x => x.IdempotencyKey)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 100)
            .WithMessage(Messages.IdempotencyKeyTooLong);
        RuleFor(x => x.ParticipantUserIds)
            .Must(ids => ids is null || ids.Count <= 500).WithMessage(Messages.ExamTooManyParticipants);
    }
}
