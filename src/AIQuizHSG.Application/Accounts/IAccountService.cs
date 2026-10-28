using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Accounts;

public interface IAccountService
{
    Task<IReadOnlyList<StudentAccount>> ListAsync(CancellationToken ct);
    Task<OperationResult<StudentAccount>> CreateAsync(CreateStudentRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<StudentAccount>> UpdateAsync(long id, UpdateStudentRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<StudentAccount>> LockAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<StudentAccount>> UnlockAsync(long id, Actor actor, CancellationToken ct);
}
