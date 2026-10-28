using AIQuizHSG.Application.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Route("api/login")]
// Đăng nhập công khai. Thành công trả token, thất bại trả 401.
public class LoginController(LoginService loginService) : ControllerBase
{
    [HttpPost]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponse>> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await loginService.LoginAsync(request, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
        if (!result.Succeeded || result.Response is null)
            return Unauthorized(new { message = result.Message });

        return Ok(result.Response);
    }
}
