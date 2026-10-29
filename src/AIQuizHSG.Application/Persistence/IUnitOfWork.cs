namespace AIQuizHSG.Application.Persistence;

// Gom mọi repository của một request. Chỉ lớp này được phép lưu xuống database.
public interface IUnitOfWork
{
    IUserRepository Users { get; }
    IRoleRepository Roles { get; }
    ISubjectRepository Subjects { get; }
    ITopicRepository Topics { get; }
    IQuestionRepository Questions { get; }
    IDifficultyLevelRepository DifficultyLevels { get; }
    IExamProfileRepository ExamProfiles { get; }
    IExamRepository Exams { get; }
    IAttemptRepository Attempts { get; }
    IAuditLogRepository AuditLogs { get; }
    ISchoolRepository Schools { get; }
    IGradeLevelRepository GradeLevels { get; }
    IClassroomRepository Classrooms { get; }
    IStudentRecordRepository StudentRecords { get; }
    IPermissionRepository Permissions { get; }
    IExamAssignmentRepository ExamAssignments { get; }
    IOmrRepository Omr { get; }
    IReportRepository Reports { get; }

    // Ghi các thay đổi đang theo dõi. Chưa commit nếu đang nằm trong transaction.
    Task<int> SaveChangesAsync(CancellationToken ct);

    // Bọc nhiều lần lưu trong một transaction, để nghiệp vụ và nhật ký cùng thành công hoặc cùng hủy.
    Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct);

    Task<bool> CanConnectAsync(CancellationToken ct);
}
