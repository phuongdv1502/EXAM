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
        RuleFor(request => request.Style)
            .Must(style => OmrFormStyles.TryNormalize(style, out _))
            .WithMessage(Messages.OmrTemplateStyleInvalid);
        RuleFor(request => request).Custom((request, context) =>
        {
            if (!OmrFormStyles.TryNormalize(request.Style, out var style))
                return;
            if (style == OmrFormStyles.NhieuPhan)
            {
                var parts = (request.Sections ?? []).Select(item => (item.Kind, item.QuestionCount)).ToList();
                if (!OmrTemplateConfig.TryFromParts(parts, out _))
                    context.AddFailure(Messages.OmrSectionsInvalid);
                return;
            }

            // Phiếu thi cổ điển luôn bốn đáp án, tối đa bốn cột mỗi cột 30 câu.
            if (style == OmrFormStyles.PhieuThi)
            {
                if (request.QuestionCount is < 1 or > 120 || request.ChoiceCount != 4)
                    context.AddFailure(Messages.OmrClassicQuestionCountInvalid);
                return;
            }

            if (request.QuestionCount is < 1 or > 60)
                context.AddFailure(nameof(request.QuestionCount), Messages.OmrQuestionCountInvalid);
            if (request.ChoiceCount is < 2 or > 6)
                context.AddFailure(nameof(request.ChoiceCount), Messages.OmrChoiceCountInvalid);
        });
    }
}

public sealed class CreateOmrBatchRequestValidator : AbstractValidator<CreateOmrBatchRequest>
{
    public CreateOmrBatchRequestValidator()
    {
        RuleFor(request => request.ExamId).GreaterThan(0);
        RuleFor(request => request.TemplateId).GreaterThan(0);
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
