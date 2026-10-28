using AIQuizHSG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIQuizHSG.Infrastructure.Data.Configurations;

internal sealed class OmrTemplateConfiguration : IEntityTypeConfiguration<OmrTemplate>
{
    public void Configure(EntityTypeBuilder<OmrTemplate> entity)
    {
        entity.ToTable("Omr_Templates");
        entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
        entity.Property(x => x.ConfigJson).HasColumnType("longtext").IsRequired();
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.HasIndex(x => new { x.Code, x.Version }).IsUnique();
        entity.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OmrImportBatchConfiguration : IEntityTypeConfiguration<OmrImportBatch>
{
    public void Configure(EntityTypeBuilder<OmrImportBatch> entity)
    {
        entity.ToTable("Omr_Import_Batches");
        entity.Property(x => x.ExamCode).HasMaxLength(32).IsRequired();
        entity.Property(x => x.SourceType).AsEnum();
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.Property(x => x.ApprovedAtUtc).AsUtc();
        entity.Property(x => x.PublishedAtUtc).AsUtc();
        entity.HasOne(x => x.Exam).WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Template).WithMany().HasForeignKey(x => x.TemplateId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.CreatedBy).WithMany().HasForeignKey(x => x.CreatedById).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OmrScannedSheetConfiguration : IEntityTypeConfiguration<OmrScannedSheet>
{
    public void Configure(EntityTypeBuilder<OmrScannedSheet> entity)
    {
        entity.ToTable("Omr_Scanned_Sheets");
        entity.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
        entity.Property(x => x.OriginalFilePath).HasMaxLength(500).IsRequired();
        entity.Property(x => x.FileHash).HasMaxLength(80).IsRequired();
        entity.Property(x => x.ContentType).HasMaxLength(100).IsRequired();
        entity.Property(x => x.RecognizedStudentCode).HasMaxLength(50);
        entity.Property(x => x.RecognizedExamCode).HasMaxLength(32);
        entity.Property(x => x.ConfirmedStudentCode).HasMaxLength(50);
        entity.Property(x => x.ConfirmedExamCode).HasMaxLength(32);
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.ReviewStatus).AsEnum();
        entity.Property(x => x.Confidence).HasPrecision(6, 4);
        entity.Property(x => x.FailureReason).HasMaxLength(1000);
        entity.Property(x => x.ReviewNote).HasMaxLength(1000);
        entity.Property(x => x.ReviewedAtUtc).AsUtc();
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.HasIndex(x => new { x.BatchId, x.FileHash }).IsUnique();
        entity.HasOne(x => x.Batch).WithMany(x => x.Sheets).HasForeignKey(x => x.BatchId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.ExamAttempt).WithMany().HasForeignKey(x => x.ExamAttemptId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.ReviewedBy).WithMany().HasForeignKey(x => x.ReviewedById).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class OmrRecognizedAnswerConfiguration : IEntityTypeConfiguration<OmrRecognizedAnswer>
{
    public void Configure(EntityTypeBuilder<OmrRecognizedAnswer> entity)
    {
        entity.ToTable("Omr_Recognized_Answers");
        entity.Property(x => x.DetectedMarksJson).HasColumnType("longtext").IsRequired();
        entity.Property(x => x.ConfirmedMarksJson).HasColumnType("longtext");
        entity.Property(x => x.Confidence).HasPrecision(6, 4);
        entity.Property(x => x.RecognitionStatus).AsEnum();
        entity.Property(x => x.ConfirmedAtUtc).AsUtc();
        entity.HasIndex(x => new { x.SheetId, x.QuestionNumber }).IsUnique();
        entity.HasOne(x => x.Sheet).WithMany(x => x.Answers).HasForeignKey(x => x.SheetId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.ExamQuestion).WithMany().HasForeignKey(x => x.ExamQuestionId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.RecognizedOption).WithMany().HasForeignKey(x => x.RecognizedOptionId).OnDelete(DeleteBehavior.Restrict);
    }
}
