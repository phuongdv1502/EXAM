using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/me")]
// Trả hồ sơ của chính người đang đăng nhập, đọc từ claim sub.
public class MeController(IUnitOfWork unitOfWork) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<UserProfile>> Get(CancellationToken ct)
    {
        var idRaw = User.FindFirst("sub")?.Value;
        if (!long.TryParse(idRaw, out var id))
            return Unauthorized(new { message = Messages.InvalidSession });

        var user = await unitOfWork.Users.FindByIdAsync(id, ct);
        if (user is null)
            return Unauthorized(new { message = Messages.AccountInactive });

        return Ok(UserMapper.ToProfile(user));
    }
}
