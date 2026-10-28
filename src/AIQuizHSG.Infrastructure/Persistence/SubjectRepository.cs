using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

public sealed class SubjectRepository(AiQuizDbContext db) : ISubjectRepository
{
    public Task<bool> AnyAsync(CancellationToken ct) => db.Subjects.AnyAsync(ct);

    public Task<List<Subject>> ListAsync(CancellationToken ct) =>
        db.Subjects.AsNoTracking().ToListAsync(ct);

    // Theo dõi để sửa tên, mã và trạng thái rồi lưu qua Unit of Work.
    public Task<Subject?> FindAsync(long id, CancellationToken ct) =>
        db.Subjects.FirstOrDefaultAsync(subject => subject.Id == id, ct);

    // Tra mã khi nhập Excel, không gắn vào context để khỏi lưu nhầm.
    public Task<Subject?> FindByCodeAsync(string code, CancellationToken ct) =>
        db.Subjects.AsNoTracking().FirstOrDefaultAsync(subject => subject.Code == code, ct);

    public async Task AddAsync(Subject subject, CancellationToken ct) =>
        await db.Subjects.AddAsync(subject, ct);
}
