using AIQuizHSG.Application.Omr;
using AIQuizHSG.Domain.Common;
using FluentValidation;

namespace AIQuizHSG.Application.Validation;

public sealed class SaveOmrTemplateRequestValidator : AbstractValidator<SaveOmrTemplateRequest>
{
    public SaveOmrTemplateRequestValidator()
    {
        RuleFor(request => request.Code).NotEmpty().WithMessage(Messages.OmrTemplateCodeRequired).MaximumLength(50);
        RuleFor(request => request.Version).GreaterThan(0);
        RuleFor(request => request.QuestionCount).InclusiveBetween(1, 60).WithMessage(Messages.OmrQuestionCountInvalid);
        RuleFor(request => request.ChoiceCount).InclusiveBetween(2, 6).WithMessage(Messages.OmrChoiceCountInvalid);
    }
}

public sealed class CreateOmrBatchRequestValidator : AbstractValidator<CreateOmrBatchRequest>
{
    public CreateOmrBatchRequestValidator()
    {
        RuleFor(request => request.ExamId).GreaterThan(0);
        RuleFor(request => request.TemplateId).GreaterThan(0);
        RuleFor(request => request.ExamCode).NotEmpty().WithMessage(Messages.OmrExamCodeRequired).MaximumLength(32);
    }
}

public sealed class ReviewOmrSheetRequestValidator : AbstractValidator<ReviewOmrSheetRequest>
{
    public ReviewOmrSheetRequestValidator()
    {
        RuleFor(request => request.StudentCode).MaximumLength(50);
        RuleFor(request => request.ExamCode).MaximumLength(32);
        RuleFor(request => request.ReviewNote).MaximumLength(1000);
        RuleFor(request => request.Answers).NotNull();
    }
}
