using FluentValidation;
using FluentValidation.Results;

namespace AIQuizHSG.Application.Validation;

// Service chỉ cần câu lỗi đầu tiên để trả OperationStatus.Invalid.
public static class ValidatorExtensions
{
    public static async Task<string?> FirstErrorAsync<T>(this IValidator<T> validator, T instance, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(instance, ct);
        return FirstMessage(result);
    }

    public static string? FirstMessage(ValidationResult result) =>
        result.IsValid ? null : result.Errors[0].ErrorMessage;
}
