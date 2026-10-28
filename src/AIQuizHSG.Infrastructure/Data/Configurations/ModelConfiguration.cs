using System.Linq.Expressions;
using AIQuizHSG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIQuizHSG.Infrastructure.Data.Configurations;

// Cột trùng tên thuộc tính. Bảng ghép thêm dấu _ giữa các từ. Enum lưu bằng chuỗi, thời điểm lưu UTC.
internal static class ColumnMapping
{
    public static PropertyBuilder<DateTime> AsUtc(this PropertyBuilder<DateTime> property) =>
        property.HasColumnType("datetime(6)").HasConversion(Keep, MarkUtc);

    public static PropertyBuilder<DateTime?> AsUtc(this PropertyBuilder<DateTime?> property) =>
        property.HasColumnType("datetime(6)").HasConversion(KeepNullable, MarkUtcNullable);

    public static PropertyBuilder<TEnum> AsEnum<TEnum>(this PropertyBuilder<TEnum> property)
        where TEnum : struct, Enum =>
        property.HasConversion<string>().HasMaxLength(32);

    private static readonly Expression<Func<DateTime, DateTime>> Keep = value => value;
    private static readonly Expression<Func<DateTime, DateTime>> MarkUtc =
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    private static readonly Expression<Func<DateTime?, DateTime?>> KeepNullable = value => value;
    private static readonly Expression<Func<DateTime?, DateTime?>> MarkUtcNullable =
        value => value.HasValue ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc) : value;
}

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> entity)
    {
        entity.ToTable("Users");
        entity.Property(x => x.Username).HasMaxLength(50).IsRequired();
        entity.HasIndex(x => x.Username).IsUnique();
        entity.Property(x => x.Email).HasMaxLength(256);
        entity.Property(x => x.PasswordHash).HasMaxLength(500).IsRequired();
        entity.Property(x => x.FullName).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.CreatedAtUtc).AsUtc();
    }
}

internal sealed class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> entity)
    {
        entity.ToTable("Roles");
        entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique();
        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
    }
}

internal sealed class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
{
    public void Configure(EntityTypeBuilder<UserRole> entity)
    {
        entity.ToTable("User_Roles");
        entity.HasKey(x => new { x.UserId, x.RoleId });
        entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class SubjectConfiguration : IEntityTypeConfiguration<Subject>
{
    public void Configure(EntityTypeBuilder<Subject> entity)
    {
        entity.ToTable("Subjects");
        entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique();
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(1000);
    }
}

internal sealed class TopicConfiguration : IEntityTypeConfiguration<Topic>
{
    public void Configure(EntityTypeBuilder<Topic> entity)
    {
        entity.ToTable("Topics");
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(1000);
        entity.HasOne(x => x.Subject).WithMany(x => x.Topics).HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentTopicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> entity)
    {
        entity.ToTable("Questions");
        entity.Property(x => x.Content).HasColumnType("longtext").IsRequired();
        entity.Property(x => x.Level).AsEnum();
        entity.Property(x => x.Type).AsEnum();
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.Source).AsEnum();
        entity.Property(x => x.Explanation).HasColumnType("longtext");
        entity.Property(x => x.RejectionReason).HasMaxLength(1000);
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.Property(x => x.UpdatedAtUtc).AsUtc();
        entity.Property(x => x.ReviewedAtUtc).AsUtc();
        entity.HasIndex(x => new { x.TopicId, x.Status, x.Level });
        entity.HasOne(x => x.Topic).WithMany(x => x.Questions).HasForeignKey(x => x.TopicId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.DifficultyLevel).WithMany().HasForeignKey(x => x.DifficultyLevelId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class QuestionOptionConfiguration : IEntityTypeConfiguration<QuestionOption>
{
    public void Configure(EntityTypeBuilder<QuestionOption> entity)
    {
        entity.ToTable("Question_Options");
        entity.Property(x => x.Content).HasColumnType("longtext").IsRequired();
        entity.HasOne(x => x.Question).WithMany(x => x.Options).HasForeignKey(x => x.QuestionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class ExamConfiguration : IEntityTypeConfiguration<Exam>
{
    public void Configure(EntityTypeBuilder<Exam> entity)
    {
        entity.ToTable("Exams");
        entity.Property(x => x.Title).HasMaxLength(300).IsRequired();
        entity.Property(x => x.Description).HasColumnType("longtext");
        entity.Property(x => x.OpensAtUtc).AsUtc();
        entity.Property(x => x.ClosesAtUtc).AsUtc();
        entity.Property(x => x.GenerationAlgorithmVersion).HasMaxLength(50);
        entity.Property(x => x.GenerationRequestKey).HasMaxLength(100);
        entity.HasIndex(x => x.GenerationRequestKey).IsUnique();
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.Property(x => x.PublishedAtUtc).AsUtc();
        entity.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.DifficultyProfile).WithMany().HasForeignKey(x => x.DifficultyProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExamQuestionConfiguration : IEntityTypeConfiguration<ExamQuestion>
{
    public void Configure(EntityTypeBuilder<ExamQuestion> entity)
    {
        entity.ToTable("Exam_Questions");
        entity.Property(x => x.ContentSnapshot).HasColumnType("longtext").IsRequired();
        entity.Property(x => x.Type).AsEnum();
        entity.Property(x => x.Level).AsEnum();
        entity.Property(x => x.Points).HasPrecision(8, 2);
        entity.HasIndex(x => new { x.ExamId, x.SourceQuestionId }).IsUnique();
        entity.HasOne(x => x.Exam).WithMany(x => x.Questions).HasForeignKey(x => x.ExamId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.SourceQuestion).WithMany().HasForeignKey(x => x.SourceQuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExamOptionConfiguration : IEntityTypeConfiguration<ExamOption>
{
    public void Configure(EntityTypeBuilder<ExamOption> entity)
    {
        entity.ToTable("Exam_Options");
        entity.Property(x => x.ContentSnapshot).HasColumnType("longtext").IsRequired();
        entity.HasAlternateKey(x => new { x.Id, x.ExamQuestionId });
        entity.HasOne(x => x.ExamQuestion).WithMany(x => x.Options).HasForeignKey(x => x.ExamQuestionId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.SourceOption).WithMany().HasForeignKey(x => x.SourceOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExamParticipantConfiguration : IEntityTypeConfiguration<ExamParticipant>
{
    public void Configure(EntityTypeBuilder<ExamParticipant> entity)
    {
        entity.ToTable("Exam_Participants");
        entity.HasIndex(x => new { x.ExamId, x.UserId }).IsUnique();
        entity.HasOne(x => x.Exam).WithMany(x => x.Participants).HasForeignKey(x => x.ExamId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExamAttemptConfiguration : IEntityTypeConfiguration<ExamAttempt>
{
    public void Configure(EntityTypeBuilder<ExamAttempt> entity)
    {
        entity.ToTable("Exam_Attempts");
        entity.Property(x => x.StartedAtUtc).AsUtc();
        entity.Property(x => x.DueAtUtc).AsUtc();
        entity.Property(x => x.SubmittedAtUtc).AsUtc();
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.Score).HasPrecision(8, 2);
        entity.Property(x => x.MaxScore).HasPrecision(8, 2);
        entity.HasIndex(x => new { x.ExamId, x.UserId, x.AttemptNumber }).IsUnique();
        entity.HasOne(x => x.Exam).WithMany().HasForeignKey(x => x.ExamId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AttemptAnswerConfiguration : IEntityTypeConfiguration<AttemptAnswer>
{
    public void Configure(EntityTypeBuilder<AttemptAnswer> entity)
    {
        entity.ToTable("Attempt_Answers");
        entity.Property(x => x.AnswerText).HasColumnType("longtext");
        entity.Property(x => x.UpdatedAtUtc).AsUtc();
        entity.HasIndex(x => new { x.AttemptId, x.ExamQuestionId }).IsUnique();
        entity.HasAlternateKey(x => new { x.Id, x.ExamQuestionId });
        entity.HasOne(x => x.Attempt).WithMany(x => x.Answers).HasForeignKey(x => x.AttemptId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.ExamQuestion).WithMany().HasForeignKey(x => x.ExamQuestionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AttemptAnswerOptionConfiguration : IEntityTypeConfiguration<AttemptAnswerOption>
{
    public void Configure(EntityTypeBuilder<AttemptAnswerOption> entity)
    {
        entity.ToTable("Attempt_Answer_Options");
        entity.HasKey(x => new { x.AttemptAnswerId, x.ExamOptionId });
        entity.HasOne(x => x.AttemptAnswer).WithMany(x => x.SelectedOptions)
            .HasForeignKey(x => new { x.AttemptAnswerId, x.ExamQuestionId })
            .HasPrincipalKey(x => new { x.Id, x.ExamQuestionId })
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.ExamOption).WithMany()
            .HasForeignKey(x => new { x.ExamOptionId, x.ExamQuestionId })
            .HasPrincipalKey(x => new { x.Id, x.ExamQuestionId })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AiQuestionRequestConfiguration : IEntityTypeConfiguration<AiQuestionRequest>
{
    public void Configure(EntityTypeBuilder<AiQuestionRequest> entity)
    {
        entity.ToTable("Ai_Question_Requests");
        entity.Property(x => x.Level).AsEnum();
        entity.Property(x => x.Type).AsEnum();
        entity.Property(x => x.SourceMaterial).HasColumnType("longtext");
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.ResultJson).HasColumnType("longtext");
        entity.Property(x => x.ErrorMessage).HasColumnType("longtext");
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.Property(x => x.CompletedAtUtc).AsUtc();
        entity.HasOne(x => x.RequestedBy).WithMany().HasForeignKey(x => x.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Topic).WithMany().HasForeignKey(x => x.TopicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AiAnalysisReportConfiguration : IEntityTypeConfiguration<AiAnalysisReport>
{
    public void Configure(EntityTypeBuilder<AiAnalysisReport> entity)
    {
        entity.ToTable("Ai_Analysis_Reports");
        entity.Property(x => x.Content).HasColumnType("longtext");
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.ErrorMessage).HasColumnType("longtext");
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.HasOne(x => x.Exam).WithMany().HasForeignKey(x => x.ExamId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Attempt).WithMany().HasForeignKey(x => x.AttemptId)
            .OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.RequestedBy).WithMany().HasForeignKey(x => x.RequestedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> entity)
    {
        entity.ToTable("Audit_Logs");
        entity.Property(x => x.Action).HasMaxLength(100).IsRequired();
        entity.Property(x => x.EntityName).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Details).HasColumnType("longtext");
        entity.Property(x => x.IpAddress).HasMaxLength(64);
        entity.Property(x => x.OccurredAtUtc).AsUtc();
        entity.HasIndex(x => x.OccurredAtUtc);
        entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class DifficultyLevelConfiguration : IEntityTypeConfiguration<DifficultyLevel>
{
    public void Configure(EntityTypeBuilder<DifficultyLevel> entity)
    {
        entity.ToTable("Difficulty_Levels");
        entity.Property(x => x.Id).ValueGeneratedNever();
        entity.Property(x => x.Code).HasMaxLength(30).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique();
        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(500);
    }
}

internal sealed class ExamDifficultyProfileConfiguration : IEntityTypeConfiguration<ExamDifficultyProfile>
{
    public void Configure(EntityTypeBuilder<ExamDifficultyProfile> entity)
    {
        entity.ToTable("Exam_Difficulty_Profiles", table =>
        {
            table.HasCheckConstraint("CK_ExamDifficultyProfile_Target", "`TargetDifficulty` BETWEEN 1 AND 5");
            table.HasCheckConstraint("CK_ExamDifficultyProfile_BaseCount", "`BaseQuestionCount` > 0");
        });
        entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        entity.Property(x => x.ScalingMode).AsEnum();
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.Property(x => x.UpdatedAtUtc).AsUtc();
        entity.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExamDifficultyProfileLevelConfiguration : IEntityTypeConfiguration<ExamDifficultyProfileLevel>
{
    public void Configure(EntityTypeBuilder<ExamDifficultyProfileLevel> entity)
    {
        entity.ToTable("Exam_Difficulty_Profile_Levels", table =>
        {
            table.HasCheckConstraint("CK_ExamDifficultyProfileLevel_Count", "`BaseQuestionCount` >= 0");
            table.HasCheckConstraint("CK_ExamDifficultyProfileLevel_Ratio", "`Ratio` >= 0 AND `Ratio` <= 1");
        });
        entity.Property(x => x.Ratio).HasPrecision(8, 6);
        entity.HasIndex(x => new { x.ProfileId, x.DifficultyLevelId }).IsUnique();
        entity.HasOne(x => x.Profile).WithMany(x => x.Levels).HasForeignKey(x => x.ProfileId)
            .OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.DifficultyLevel).WithMany().HasForeignKey(x => x.DifficultyLevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
