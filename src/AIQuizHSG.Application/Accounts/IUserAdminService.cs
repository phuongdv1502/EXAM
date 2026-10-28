using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Accounts;

public interface IUserAdminService
{
    Task<OperationResult<AccountDirectory>> ListAsync(Actor actor, CancellationToken ct);
    Task<OperationResult<AccountResponse>> CreateAsync(CreateAccountRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<AccountResponse>> UpdateAsync(long id, UpdateAccountRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<AccountResponse>> LockAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<AccountResponse>> UnlockAsync(long id, Actor actor, CancellationToken ct);
}
