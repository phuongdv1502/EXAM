using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Domain.Authorization;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIQuizHSG.Infrastructure.Seeding;

// Mỗi nhóm chỉ tạo khi bảng còn trống, nên chạy lại không ghi đè dữ liệu đã có.
public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        AiQuizDbContext db,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwords,
        IConfiguration config,
        ILogger logger,
        CancellationToken ct)
    {
        // Migration vẫn đi qua DbContext vì đây là việc tạo schema, không phải nghiệp vụ.
        await db.Database.MigrateAsync(ct);

        if (!await unitOfWork.Roles.AnyAsync(ct))
        {
            await unitOfWork.Roles.AddRangeAsync(
            [
                new Role { Code = RoleCodes.Admin, Name = "Quản trị hệ thống" },
                new Role { Code = RoleCodes.Teacher, Name = "Giáo viên" },
                new Role { Code = RoleCodes.Student, Name = "Học sinh" }
            ], ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        if (!await unitOfWork.Users.AnyAsync(ct))
        {
            var username = config["Seed:Username"] ?? "admin";
            var password = config["Seed:Password"];
            if (string.IsNullOrWhiteSpace(password))
                throw new InvalidOperationException("Thiếu Seed:Password để tạo tài khoản quản trị đầu tiên.");

            var role = await unitOfWork.Roles.FindByCodeAsync(RoleCodes.Admin, ct)
                ?? throw new InvalidOperationException("Thiếu vai trò ADMIN.");
            var admin = new User
            {
                Username = username.Trim(),
                Email = config["Seed:Email"],
                FullName = config["Seed:FullName"] ?? "Quản trị hệ thống",
                PasswordHash = passwords.Hash(password),
                Status = UserStatus.Active,
                CreatedAtUtc = DateTime.UtcNow,
                UserRoles = [new UserRole { RoleId = role.Id }]
            };
            await unitOfWork.Users.AddAsync(admin, ct);
            await unitOfWork.SaveChangesAsync(ct);
            logger.LogInformation("Đã tạo tài khoản quản trị {Username}.", admin.Username);
        }

        if (!await unitOfWork.Subjects.AnyAsync(ct))
        {
            await unitOfWork.Subjects.AddAsync(new Subject
            {
                Code = "TIN",
                Name = "Tin học",
                Description = "Môn Tin học THCS",
                IsActive = true
            }, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        await SeedDifficultyAsync(unitOfWork, config, clock: DateTime.UtcNow, ct);
        await SeedOrganizationAsync(unitOfWork, ct);
    }

    // Khối 6–9, vai trò quản trị trường và quyền mẫu. Chạy lại không ghi đè dữ liệu đã có.
    private static async Task SeedOrganizationAsync(IUnitOfWork unitOfWork, CancellationToken ct)
    {
        if (await unitOfWork.Roles.FindByCodeAsync(RoleCodes.SchoolAdmin, ct) is null)
        {
            await unitOfWork.Roles.AddRangeAsync(
                [new Role { Code = RoleCodes.SchoolAdmin, Name = "Quản trị trường" }], ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        if (!await unitOfWork.GradeLevels.AnyAsync(ct))
        {
            await unitOfWork.GradeLevels.AddRangeAsync(
            [
                new GradeLevel { Code = "6", Name = "Khối 6", EducationLevel = "THCS", SortOrder = 6, IsActive = true },
                new GradeLevel { Code = "7", Name = "Khối 7", EducationLevel = "THCS", SortOrder = 7, IsActive = true },
                new GradeLevel { Code = "8", Name = "Khối 8", EducationLevel = "THCS", SortOrder = 8, IsActive = true },
                new GradeLevel { Code = "9", Name = "Khối 9", EducationLevel = "THCS", SortOrder = 9, IsActive = true }
            ], ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        if (!await unitOfWork.Permissions.AnyAsync(ct))
        {
            var now = DateTime.UtcNow;
            await unitOfWork.Permissions.AddRangeAsync(PermissionCatalog.All.Select(item => new Permission
            {
                Code = item.Code,
                Name = item.Name,
                Module = item.Module,
                IsSystem = true,
                IsActive = true,
                CreatedAtUtc = now
            }), ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        if (await unitOfWork.Permissions.AnyRoleLinkAsync(ct))
            return;

        var permissions = await unitOfWork.Permissions.ListAsync(null, ct);
        var byCode = permissions.ToDictionary(item => item.Code);
        var roles = await unitOfWork.Roles.ListAsync(ct);
        var links = new List<RolePermission>();
        foreach (var role in roles)
        {
            foreach (var code in PermissionCatalog.ForRole(role.Code))
            {
                if (!byCode.TryGetValue(code, out var permission))
                    continue;
                links.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permission.Id,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await unitOfWork.Permissions.AddRoleLinksAsync(links, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    // Năm mức câu hỏi và profile Khó gốc. Không gán mức cho câu đã có.
    private static async Task SeedDifficultyAsync(IUnitOfWork unitOfWork, IConfiguration config, DateTime clock, CancellationToken ct)
    {
        if (!await unitOfWork.DifficultyLevels.AnyAsync(ct))
        {
            await unitOfWork.DifficultyLevels.AddRangeAsync(
            [
                Level(1, "Nhận biết", "Nhớ và nhận diện kiến thức cơ bản."),
                Level(2, "Thông hiểu", "Giải thích, đọc hiểu, xác định kết quả."),
                Level(3, "Vận dụng", "Áp dụng kiến thức giải quyết bài toán."),
                Level(4, "Vận dụng cao", "Phân tích, kết hợp nhiều kỹ thuật."),
                Level(5, "HSG/chuyên sâu", "Tư duy thuật toán sâu, tối ưu và phân loại học sinh giỏi.")
            ], ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

        if (await unitOfWork.ExamProfiles.AnySystemDefaultAsync(ct))
            return;

        var adminName = config["Seed:Username"] ?? "admin";
        var admin = await unitOfWork.Users.FindByUsernameAsync(adminName, ct);
        if (admin is null)
            return;

        var counts = new[] { 5, 10, 18, 12, 5 };
        var profile = new ExamDifficultyProfile
        {
            Name = "Đề HSG Tin học - Mức khó",
            TargetDifficulty = 4,
            BaseQuestionCount = 50,
            ScalingMode = ScalingMode.Proportional,
            IsSystemDefault = true,
            Version = 1,
            Status = ProfileStatus.Active,
            CreatedById = admin.Id,
            CreatedAtUtc = clock,
            UpdatedAtUtc = clock
        };
        for (var index = 0; index < counts.Length; index++)
        {
            profile.Levels.Add(new ExamDifficultyProfileLevel
            {
                DifficultyLevelId = (byte)(index + 1),
                BaseQuestionCount = counts[index],
                Ratio = decimal.Round(counts[index] / 50m, 6, MidpointRounding.AwayFromZero),
                SortOrder = index + 1
            });
        }

        await unitOfWork.ExamProfiles.AddAsync(profile, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static DifficultyLevel Level(byte id, string name, string description) => new()
    {
        Id = id,
        Code = id.ToString(),
        Name = name,
        Description = description,
        SortOrder = id,
        IsActive = true
    };
}
