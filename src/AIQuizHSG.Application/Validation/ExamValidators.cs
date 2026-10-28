using AIQuizHSG.Application.Accounts;
using AIQuizHSG.Application.Exams;
using AIQuizHSG.Domain.Common;
using FluentValidation;

namespace AIQuizHSG.Application.Validation;

public sealed class SaveExamRequestValidator : AbstractValidator<SaveExamRequest>
{
    public SaveExamRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Title)
            .NotEmpty().WithMessage(Messages.ExamTitleRequired)
            .Must(title => title.Trim().Length <= 300).WithMessage(Messages.ExamTitleTooLong);

        RuleFor(x => x.Description)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 5000)
            .WithMessage(Messages.ExamDescriptionTooLong);

        RuleFor(x => x.SubjectId)
            .GreaterThan(0).WithMessage(Messages.SubjectNotSelected);

        RuleFor(x => x.DurationMinutes)
            .InclusiveBetween(1, 300).WithMessage(Messages.ExamDurationInvalid);

        RuleFor(x => x.ClosesAtUtc)
            .Must((request, closes) => closes is null || request.OpensAtUtc is null || closes > request.OpensAtUtc)
            .WithMessage(Messages.ExamWindowInvalid);

        RuleFor(x => x.MaxAttempts)
            .InclusiveBetween(1, 10).WithMessage(Messages.ExamAttemptsInvalid);

        RuleFor(x => x.Questions)
            .NotNull().WithMessage(Messages.ExamQuestionsRequired)
            .Must(items => items is { Count: > 0 }).WithMessage(Messages.ExamQuestionsRequired)
            .Must(items => items is null || items.Count <= 100).WithMessage(Messages.ExamTooManyQuestions);

        RuleForEach(x => x.Questions).ChildRules(question =>
        {
            question.RuleLevelCascadeMode = CascadeMode.Stop;
            question.RuleFor(item => item.QuestionId).GreaterThan(0).WithMessage(Messages.ExamQuestionNotApproved);
            question.RuleFor(item => item.Points).GreaterThan(0).LessThanOrEqualTo(100).WithMessage(Messages.ExamPointsInvalid);
        });

        RuleFor(x => x.ParticipantUserIds)
            .Must(ids => ids is null || ids.Count <= 500).WithMessage(Messages.ExamTooManyParticipants);

        RuleForEach(x => x.ParticipantUserIds)
            .GreaterThan(0).WithMessage(Messages.ExamParticipantInvalid);
    }
}

public sealed class SaveAnswerRequestValidator : AbstractValidator<SaveAnswerRequest>
{
    public SaveAnswerRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.OptionIds)
            .NotNull().WithMessage(Messages.AttemptOptionInvalid)
            .Must(ids => ids is null || ids.Count <= 6).WithMessage(Messages.AttemptTooManyOptions);

        RuleForEach(x => x.OptionIds)
            .GreaterThan(0).WithMessage(Messages.AttemptOptionInvalid);
    }
}

public sealed class CreateStudentRequestValidator : AbstractValidator<CreateStudentRequest>
{
    public CreateStudentRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(Messages.UsernameRequired)
            .Must(value => value.Trim().Length >= 3).WithMessage(Messages.UsernameTooShort)
            .Must(value => value.Trim().Length <= 50).WithMessage(Messages.UsernameTooLong)
            .Must(value => !value.Trim().Any(char.IsWhiteSpace)).WithMessage(Messages.UsernameInvalid);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(Messages.PasswordRequired)
            .MinimumLength(8).WithMessage(Messages.PasswordTooShort)
            .MaximumLength(100).WithMessage(Messages.PasswordTooLong);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(Messages.FullNameRequired)
            .Must(value => value.Trim().Length <= 200).WithMessage(Messages.FullNameTooLong);

        RuleFor(x => x.Email)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 256).WithMessage(Messages.EmailTooLong)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Contains('@')).WithMessage(Messages.EmailInvalid);
    }
}

public sealed class UpdateStudentRequestValidator : AbstractValidator<UpdateStudentRequest>
{
    public UpdateStudentRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(Messages.FullNameRequired)
            .Must(value => value.Trim().Length <= 200).WithMessage(Messages.FullNameTooLong);

        RuleFor(x => x.Email)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 256).WithMessage(Messages.EmailTooLong)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Contains('@')).WithMessage(Messages.EmailInvalid);

        RuleFor(x => x.Password)
            .Must(value => string.IsNullOrEmpty(value) || value.Length >= 8).WithMessage(Messages.PasswordTooShort)
            .Must(value => string.IsNullOrEmpty(value) || value.Length <= 100).WithMessage(Messages.PasswordTooLong);
    }
}

public sealed class CreateAccountRequestValidator : AbstractValidator<CreateAccountRequest>
{
    public CreateAccountRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(Messages.UsernameRequired)
            .Must(value => value.Trim().Length >= 3).WithMessage(Messages.UsernameTooShort)
            .Must(value => value.Trim().Length <= 50).WithMessage(Messages.UsernameTooLong)
            .Must(value => !value.Trim().Any(char.IsWhiteSpace)).WithMessage(Messages.UsernameInvalid);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(Messages.PasswordRequired)
            .MinimumLength(8).WithMessage(Messages.PasswordTooShort)
            .MaximumLength(100).WithMessage(Messages.PasswordTooLong);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(Messages.FullNameRequired)
            .Must(value => value.Trim().Length <= 200).WithMessage(Messages.FullNameTooLong);

        RuleFor(x => x.Email)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 256).WithMessage(Messages.EmailTooLong)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Contains('@')).WithMessage(Messages.EmailInvalid);

        RuleFor(x => x.RoleIds)
            .NotNull().WithMessage(Messages.RoleRequired)
            .Must(ids => ids.Count > 0).WithMessage(Messages.RoleRequired);
    }
}

public sealed class UpdateAccountRequestValidator : AbstractValidator<UpdateAccountRequest>
{
    public UpdateAccountRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage(Messages.FullNameRequired)
            .Must(value => value.Trim().Length <= 200).WithMessage(Messages.FullNameTooLong);

        RuleFor(x => x.Email)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 256).WithMessage(Messages.EmailTooLong)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Contains('@')).WithMessage(Messages.EmailInvalid);

        RuleFor(x => x.Password)
            .Must(value => string.IsNullOrEmpty(value) || value.Length >= 8).WithMessage(Messages.PasswordTooShort)
            .Must(value => string.IsNullOrEmpty(value) || value.Length <= 100).WithMessage(Messages.PasswordTooLong);
    }
}
