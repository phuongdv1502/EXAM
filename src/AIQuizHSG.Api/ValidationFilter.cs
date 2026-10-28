using AIQuizHSG.Application.Validation;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AIQuizHSG.Api;

// Tìm validator đúng kiểu body. Sai thì trả 400 kèm danh sách field, không chạy action.
public sealed class ValidationFilter : IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        foreach (var argument in context.ActionArguments.Values)
        {
            if (argument is null)
                continue;

            var validatorType = typeof(IValidator<>).MakeGenericType(argument.GetType());
            if (context.HttpContext.RequestServices.GetService(validatorType) is not IValidator validator)
                continue;

            var validationContext = new ValidationContext<object>(argument);
            var result = await validator.ValidateAsync(validationContext, context.HttpContext.RequestAborted);
            var message = ValidatorExtensions.FirstMessage(result);
            if (message is null)
                continue;

            context.Result = new BadRequestObjectResult(new
            {
                message,
                errors = result.Errors.Select(error => new { field = error.PropertyName, message = error.ErrorMessage })
            });
            return;
        }

        await next();
    }
}
