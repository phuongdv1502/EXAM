using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;

namespace AIQuizHSG.Api.Security;

// Sau khi JWT hợp lệ, đọc lại tài khoản trong database. Khóa hoặc xóa thì cắt phiên ngay.
public sealed class ActiveAccountMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IUnitOfWork unitOfWork)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var idRaw = context.User.FindFirst("sub")?.Value;
            if (!long.TryParse(idRaw, out var id))
            {
                await Reject(context, Messages.InvalidSession);
                return;
            }

            var user = await unitOfWork.Users.FindByIdAsync(id, context.RequestAborted);
            if (user is null || user.Status != UserStatus.Active)
            {
                await Reject(context, Messages.AccountInactive);
                return;
            }
        }

        await next(context);
    }

    private static Task Reject(HttpContext context, string message)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return context.Response.WriteAsJsonAsync(new { message });
    }
}
