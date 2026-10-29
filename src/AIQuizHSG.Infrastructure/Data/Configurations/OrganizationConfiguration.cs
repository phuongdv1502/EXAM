using AIQuizHSG.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AIQuizHSG.Infrastructure.Data.Configurations;

internal sealed class SchoolConfiguration : IEntityTypeConfiguration<School>
{
    public void Configure(EntityTypeBuilder<School> entity)
    {
        entity.ToTable("Schools");
        entity.Property(x => x.Code).HasMaxLength(50).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique();
        entity.Property(x => x.Name).HasMaxLength(255).IsRequired();
        entity.Property(x => x.Address).HasMaxLength(500);
        entity.Property(x => x.ProvinceCode).HasMaxLength(20);
        entity.Property(x => x.DistrictCode).HasMaxLength(20);
        entity.Property(x => x.Phone).HasMaxLength(30);
        entity.Property(x => x.Email).HasMaxLength(255);
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.Property(x => x.UpdatedAtUtc).AsUtc();
    }
}

internal sealed class SchoolYearConfiguration : IEntityTypeConfiguration<SchoolYear>
{
    public void Configure(EntityTypeBuilder<SchoolYear> entity)
    {
        entity.ToTable("School_Years", table =>
        {
            table.HasCheckConstraint("CK_School_Years_Dates", "`EndDate` >= `StartDate`");
        });
        entity.Property(x => x.Name).HasMaxLength(30).IsRequired();
        entity.Property(x => x.StartDate).HasColumnType("date");
        entity.Property(x => x.EndDate).HasColumnType("date");
        entity.Property(x => x.Status).AsEnum();
        entity.HasIndex(x => new { x.SchoolId, x.Name }).IsUnique();
        entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class GradeLevelConfiguration : IEntityTypeConfiguration<GradeLevel>
{
    public void Configure(EntityTypeBuilder<GradeLevel> entity)
    {
        entity.ToTable("Grade_Levels");
        entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique();
        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        entity.Property(x => x.EducationLevel).HasMaxLength(30).IsRequired();
    }
}

internal sealed class ClassroomConfiguration : IEntityTypeConfiguration<Classroom>
{
    public void Configure(EntityTypeBuilder<Classroom> entity)
    {
        entity.ToTable("Classrooms", table =>
        {
            table.HasCheckConstraint("CK_Classrooms_Capacity", "`Capacity` IS NULL OR `Capacity` > 0");
        });
        entity.Property(x => x.ClassCode).HasMaxLength(30).IsRequired();
        entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
        entity.Property(x => x.Status).AsEnum();
        entity.HasIndex(x => new { x.SchoolYearId, x.ClassCode }).IsUnique();
        entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.SchoolYear).WithMany().HasForeignKey(x => x.SchoolYearId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.GradeLevel).WithMany().HasForeignKey(x => x.GradeLevelId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.HomeroomTeacher).WithMany().HasForeignKey(x => x.HomeroomTeacherUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class StudentProfileConfiguration : IEntityTypeConfiguration<StudentProfile>
{
    public void Configure(EntityTypeBuilder<StudentProfile> entity)
    {
        entity.ToTable("Students");
        entity.Property(x => x.StudentCode).HasMaxLength(50).IsRequired();
        entity.Property(x => x.FullName).HasMaxLength(255).IsRequired();
        entity.Property(x => x.DateOfBirth).HasColumnType("date");
        entity.Property(x => x.Gender).HasConversion<string>().HasMaxLength(32);
        entity.Property(x => x.Email).HasMaxLength(255);
        entity.Property(x => x.Phone).HasMaxLength(30);
        entity.Property(x => x.Address).HasMaxLength(500);
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.Property(x => x.UpdatedAtUtc).AsUtc();
        entity.HasIndex(x => new { x.SchoolId, x.StudentCode }).IsUnique();
        entity.HasIndex(x => x.UserId).IsUnique();
        entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class StudentEnrollmentConfiguration : IEntityTypeConfiguration<StudentEnrollment>
{
    public void Configure(EntityTypeBuilder<StudentEnrollment> entity)
    {
        entity.ToTable("Student_Enrollments", table =>
        {
            table.HasCheckConstraint("CK_Student_Enrollments_Leave", "`LeaveDate` IS NULL OR `LeaveDate` >= `EnrollmentDate`");
        });
        entity.Property(x => x.EnrollmentDate).HasColumnType("date");
        entity.Property(x => x.LeaveDate).HasColumnType("date");
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.Note).HasMaxLength(500);
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.HasIndex(x => x.StudentId);
        entity.HasIndex(x => x.ClassroomId);
        entity.HasOne(x => x.Student).WithMany(x => x.Enrollments).HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Classroom).WithMany().HasForeignKey(x => x.ClassroomId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class SchoolMembershipConfiguration : IEntityTypeConfiguration<SchoolMembership>
{
    public void Configure(EntityTypeBuilder<SchoolMembership> entity)
    {
        entity.ToTable("School_Memberships");
        entity.Property(x => x.SchoolRole).AsEnum();
        entity.Property(x => x.Status).AsEnum();
        entity.Property(x => x.StartDate).HasColumnType("date");
        entity.Property(x => x.EndDate).HasColumnType("date");
        entity.HasIndex(x => new { x.SchoolId, x.UserId, x.SchoolRole }).IsUnique();
        entity.HasOne(x => x.School).WithMany().HasForeignKey(x => x.SchoolId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ClassMembershipConfiguration : IEntityTypeConfiguration<ClassMembership>
{
    public void Configure(EntityTypeBuilder<ClassMembership> entity)
    {
        entity.ToTable("Class_Memberships");
        entity.Property(x => x.ClassRole).AsEnum();
        entity.HasIndex(x => new { x.ClassroomId, x.UserId, x.ClassRole, x.SubjectId }).IsUnique();
        entity.HasOne(x => x.Classroom).WithMany().HasForeignKey(x => x.ClassroomId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class TeacherSubjectConfiguration : IEntityTypeConfiguration<TeacherSubject>
{
    public void Configure(EntityTypeBuilder<TeacherSubject> entity)
    {
        entity.ToTable("Teacher_Subjects");
        entity.HasKey(x => new { x.UserId, x.SubjectId });
        entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Subject).WithMany().HasForeignKey(x => x.SubjectId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExamAssignmentConfiguration : IEntityTypeConfiguration<ExamAssignment>
{
    public void Configure(EntityTypeBuilder<ExamAssignment> entity)
    {
        entity.ToTable("Exam_Assignments", table =>
        {
            table.HasCheckConstraint(
                "CK_Exam_Assignments_Target",
                "(`ClassroomId` IS NULL) <> (`StudentId` IS NULL)");
        });
        entity.Property(x => x.AssignedAtUtc).AsUtc();
        entity.Property(x => x.DueAtUtc).AsUtc();
        entity.Property(x => x.Status).AsEnum();
        entity.HasIndex(x => new { x.ExamId, x.ClassroomId, x.StudentId }).IsUnique();
        entity.HasOne(x => x.Exam).WithMany().HasForeignKey(x => x.ExamId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Classroom).WithMany().HasForeignKey(x => x.ClassroomId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne(x => x.Student).WithMany().HasForeignKey(x => x.StudentId).OnDelete(DeleteBehavior.Restrict);
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> entity)
    {
        entity.ToTable("Permissions");
        entity.Property(x => x.Code).HasMaxLength(100).IsRequired();
        entity.HasIndex(x => x.Code).IsUnique();
        entity.Property(x => x.Name).HasMaxLength(150).IsRequired();
        entity.Property(x => x.Module).HasMaxLength(80).IsRequired();
        entity.Property(x => x.Description).HasMaxLength(500);
        entity.Property(x => x.CreatedAtUtc).AsUtc();
    }
}

internal sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> entity)
    {
        entity.ToTable("Role_Permissions");
        entity.HasKey(x => new { x.RoleId, x.PermissionId });
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class UserPermissionConfiguration : IEntityTypeConfiguration<UserPermission>
{
    public void Configure(EntityTypeBuilder<UserPermission> entity)
    {
        entity.ToTable("User_Permissions", table =>
            table.HasCheckConstraint("CK_User_Permissions_Effect", "`Effect` IN ('Allow', 'Deny')"));
        entity.HasKey(x => new { x.UserId, x.PermissionId });
        entity.Property(x => x.Effect).AsEnum();
        entity.Property(x => x.CreatedAtUtc).AsUtc();
        entity.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne(x => x.Permission).WithMany().HasForeignKey(x => x.PermissionId).OnDelete(DeleteBehavior.Cascade);
        entity.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.SetNull);
    }
}
