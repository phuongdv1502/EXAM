using AIQuizHSG.Api;
using AIQuizHSG.Application.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/accounts")]
// Tạo, sửa và khóa tài khoản. Quyền user.view quyết định ai được vào.
public class AccountsController(IUserAdminService users) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<AccountDirectory>> List(CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await users.ListAsync(actor, ct));
    }

    [HttpPost]
    public async Task<ActionResult<AccountResponse>> Create(CreateAccountRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await users.CreateAsync(request, actor, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<AccountResponse>> Update(long id, UpdateAccountRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await users.UpdateAsync(id, request, actor, ct));
    }

    [HttpPost("{id:long}/lock")]
    public async Task<ActionResult<AccountResponse>> Lock(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await users.LockAsync(id, actor, ct));
    }

    [HttpPost("{id:long}/unlock")]
    public async Task<ActionResult<AccountResponse>> Unlock(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await users.UnlockAsync(id, actor, ct));
    }
}
