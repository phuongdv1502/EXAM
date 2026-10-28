using AIQuizHSG.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AIQuizHSG.Infrastructure.Data;

// Kết nối MySQL. Tên bảng và cột tiếng Anh khai báo trong Data/Configurations.
public class AiQuizDbContext(DbContextOptions<AiQuizDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Subject> Subjects => Set<Subject>();
    public DbSet<Topic> Topics => Set<Topic>();
    public DbSet<Question> Questions => Set<Question>();
    public DbSet<QuestionOption> QuestionOptions => Set<QuestionOption>();
    public DbSet<DifficultyLevel> DifficultyLevels => Set<DifficultyLevel>();
    public DbSet<ExamDifficultyProfile> ExamDifficultyProfiles => Set<ExamDifficultyProfile>();
    public DbSet<ExamDifficultyProfileLevel> ExamDifficultyProfileLevels => Set<ExamDifficultyProfileLevel>();
    public DbSet<Exam> Exams => Set<Exam>();
    public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
    public DbSet<ExamOption> ExamOptions => Set<ExamOption>();
    public DbSet<ExamParticipant> ExamParticipants => Set<ExamParticipant>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();
    public DbSet<AttemptAnswer> AttemptAnswers => Set<AttemptAnswer>();
    public DbSet<AttemptAnswerOption> AttemptAnswerOptions => Set<AttemptAnswerOption>();
    public DbSet<AiQuestionRequest> AiQuestionRequests => Set<AiQuestionRequest>();
    public DbSet<AiAnalysisReport> AiAnalysisReports => Set<AiAnalysisReport>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<School> Schools => Set<School>();
    public DbSet<SchoolYear> SchoolYears => Set<SchoolYear>();
    public DbSet<GradeLevel> GradeLevels => Set<GradeLevel>();
    public DbSet<Classroom> Classrooms => Set<Classroom>();
    public DbSet<StudentProfile> Students => Set<StudentProfile>();
    public DbSet<StudentEnrollment> StudentEnrollments => Set<StudentEnrollment>();
    public DbSet<SchoolMembership> SchoolMemberships => Set<SchoolMembership>();
    public DbSet<ClassMembership> ClassMemberships => Set<ClassMembership>();
    public DbSet<ExamAssignment> ExamAssignments => Set<ExamAssignment>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserPermission> UserPermissions => Set<UserPermission>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasCharSet("utf8mb4");
        modelBuilder.UseCollation("utf8mb4_unicode_ci");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AiQuizDbContext).Assembly);
    }
}
