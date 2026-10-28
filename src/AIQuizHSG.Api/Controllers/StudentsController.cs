using AIQuizHSG.Api;
using AIQuizHSG.Application.Accounts;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize(Policy = RoleCodes.Admin)]
[Route("api/students")]
// Quản trị tạo, sửa và khóa tài khoản học sinh.
public class StudentsController(AccountService accounts) : ControllerBase
{
    [HttpGet]
    public async Task<IReadOnlyList<StudentAccount>> List(CancellationToken ct) =>
        await accounts.ListAsync(ct);

    [HttpPost]
    public async Task<ActionResult<StudentAccount>> Create(CreateStudentRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await accounts.CreateAsync(request, actor, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<StudentAccount>> Update(long id, UpdateStudentRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await accounts.UpdateAsync(id, request, actor, ct));
    }

    [HttpPost("{id:long}/lock")]
    public async Task<ActionResult<StudentAccount>> Lock(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await accounts.LockAsync(id, actor, ct));
    }

    [HttpPost("{id:long}/unlock")]
    public async Task<ActionResult<StudentAccount>> Unlock(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await accounts.UnlockAsync(id, actor, ct));
    }
}
