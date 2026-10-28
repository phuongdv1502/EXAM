using AIQuizHSG.Application.Common;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api;

// Lấy mã người dùng từ claim sub của JWT để ghi nhật ký.
internal static class Actors
{
    public static bool TryGet(HttpContext context, out Actor actor)
    {
        if (!long.TryParse(context.User.FindFirst("sub")?.Value, out var id))
        {
            actor = new Actor(0, null);
            return false;
        }

        actor = new Actor(id, context.Connection.RemoteIpAddress?.ToString());
        return true;
    }
}

// Đổi kết quả nghiệp vụ thành HTTP: thành công 200, không thấy 404, còn lại 400.
internal static class ApiResults
{
    public static ActionResult From<T>(OperationResult<T> result) =>
        result.Status switch
        {
            OperationStatus.Ok => new OkObjectResult(result.Value),
            OperationStatus.NotFound => new NotFoundObjectResult(new { message = result.Message }),
            OperationStatus.Forbidden => new ObjectResult(new { message = result.Message }) { StatusCode = StatusCodes.Status403Forbidden },
            _ => new BadRequestObjectResult(new { message = result.Message })
        };

    public static ActionResult Unauthorized() =>
        new UnauthorizedObjectResult(new { message = Messages.InvalidSession });
}
