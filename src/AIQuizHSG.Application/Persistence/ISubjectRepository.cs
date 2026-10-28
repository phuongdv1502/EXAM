using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Môn học. Bản ghi cần sửa thì được theo dõi, tra mã khi nhập Excel thì không.
public interface ISubjectRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<List<Subject>> ListAsync(CancellationToken ct);
    Task<Subject?> FindAsync(long id, CancellationToken ct);
    Task<Subject?> FindByCodeAsync(string code, CancellationToken ct);
    Task AddAsync(Subject subject, CancellationToken ct);
}
