using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Persistence;

// Một DbContext cho cả request. Mọi repository dùng chung context này.
public sealed class UnitOfWork(AiQuizDbContext db) : IUnitOfWork
{
    public IUserRepository Users { get; } = new UserRepository(db);
    public IRoleRepository Roles { get; } = new RoleRepository(db);
    public ISubjectRepository Subjects { get; } = new SubjectRepository(db);
    public ITopicRepository Topics { get; } = new TopicRepository(db);
    public IQuestionRepository Questions { get; } = new QuestionRepository(db);
    public IDifficultyLevelRepository DifficultyLevels { get; } = new DifficultyLevelRepository(db);
    public IExamProfileRepository ExamProfiles { get; } = new ExamProfileRepository(db);
    public IExamRepository Exams { get; } = new ExamRepository(db);
    public IAttemptRepository Attempts { get; } = new AttemptRepository(db);
    public IAuditLogRepository AuditLogs { get; } = new AuditLogRepository(db);
    public ISchoolRepository Schools { get; } = new SchoolRepository(db);
    public IGradeLevelRepository GradeLevels { get; } = new GradeLevelRepository(db);
    public IClassroomRepository Classrooms { get; } = new ClassroomRepository(db);
    public IStudentRecordRepository StudentRecords { get; } = new StudentRecordRepository(db);
    public IPermissionRepository Permissions { get; } = new PermissionRepository(db);
    public IExamAssignmentRepository ExamAssignments { get; } = new ExamAssignmentRepository(db);
    public IOmrRepository Omr { get; } = new OmrRepository(db);
    public IReportRepository Reports { get; } = new ReportRepository(db);

    public Task<int> SaveChangesAsync(CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task<bool> CanConnectAsync(CancellationToken ct) => db.Database.CanConnectAsync(ct);

    // EnableRetryOnFailure bắt transaction phải chạy qua execution strategy.
    public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct)
    {
        var strategy = db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await db.Database.BeginTransactionAsync(ct);
            await action(ct);
            await transaction.CommitAsync(ct);
        });
    }
}
