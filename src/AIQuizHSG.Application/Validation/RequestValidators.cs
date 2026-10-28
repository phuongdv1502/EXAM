using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Questions;
using FluentValidation;

namespace AIQuizHSG.Application.Validation;

// Dừng ở lỗi đầu tiên của từng trường, không gom hết mọi lỗi của cùng một ô.
public sealed class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Username)
            .NotEmpty().WithMessage(Messages.UsernameRequired);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage(Messages.PasswordRequired);
    }
}

public sealed class SaveSubjectRequestValidator : AbstractValidator<SaveSubjectRequest>
{
    public SaveSubjectRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Code)
            .NotEmpty().WithMessage(Messages.SubjectCodeRequired)
            .Must(code => code.Trim().Length <= 50).WithMessage(Messages.SubjectCodeTooLong);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(Messages.SubjectNameRequired)
            .Must(name => name.Trim().Length <= 200).WithMessage(Messages.SubjectNameTooLong);

        RuleFor(x => x.Description)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 1000)
            .WithMessage(Messages.DescriptionTooLong);
    }
}

public sealed class SaveTopicRequestValidator : AbstractValidator<SaveTopicRequest>
{
    public SaveTopicRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.SubjectId)
            .GreaterThan(0).WithMessage(Messages.SubjectNotSelected);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage(Messages.TopicNameRequired)
            .Must(name => name.Trim().Length <= 200).WithMessage(Messages.TopicNameTooLong);

        RuleFor(x => x.Description)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 1000)
            .WithMessage(Messages.DescriptionTooLong);
    }
}

public sealed class SaveQuestionRequestValidator : AbstractValidator<SaveQuestionRequest>
{
    public SaveQuestionRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.TopicId)
            .GreaterThan(0).WithMessage(Messages.TopicNotSelected);

        RuleFor(x => x.Content)
            .NotEmpty().WithMessage(Messages.QuestionContentRequired)
            .Must(content => content.Trim().Length <= 20000).WithMessage(Messages.QuestionContentTooLong);

        RuleFor(x => x.Level)
            .IsInEnum().WithMessage(Messages.LevelInvalid);

        RuleFor(x => x.Type)
            .IsInEnum().WithMessage(Messages.TypeInvalid);

        RuleFor(x => x.Explanation)
            .Must(value => string.IsNullOrWhiteSpace(value) || value.Trim().Length <= 20000)
            .WithMessage(Messages.ExplanationTooLong);

        // Số phương án và số đáp án đúng nằm ở QuestionRules, không lặp lại ở đây.
        RuleFor(x => x.Options)
            .NotNull().WithMessage(Messages.OptionsRequired)
            .Custom((options, context) =>
            {
                if (options is null)
                    return;

                var request = (SaveQuestionRequest)context.InstanceToValidate;
                var message = QuestionRules.ValidateOptions(
                    request.Type,
                    options.Select(option => (option.Content, option.IsCorrect)).ToArray());
                if (message is not null)
                    context.AddFailure(message);
            });
    }
}

public sealed class RejectQuestionRequestValidator : AbstractValidator<RejectQuestionRequest>
{
    public RejectQuestionRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;

        RuleFor(x => x.Reason)
            .NotEmpty().WithMessage(Messages.RejectReasonRequired)
            .Must(reason => reason.Trim().Length <= 1000).WithMessage(Messages.RejectReasonTooLong);
    }
}
