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
        await SeedTopicsAsync(db, unitOfWork, ct);
        await SeedSchoolSampleAsync(db, unitOfWork, passwords, config, ct);
        await SeedQuestionsAsync(db, unitOfWork, config, ct);
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

    // Chuyên đề Tin học dùng khi tạo câu và sinh đề. Chỉ tạo khi bảng còn trống.
    private static async Task SeedTopicsAsync(AiQuizDbContext db, IUnitOfWork unitOfWork, CancellationToken ct)
    {
        if (await db.Topics.AnyAsync(ct))
            return;
        var subject = await unitOfWork.Subjects.FindByCodeAsync("TIN", ct);
        if (subject is null)
            return;

        var roots = new (string Name, int Sort)[]
        {
            ("Biểu diễn thông tin", 1),
            ("Thuật toán", 2),
            ("Ngôn ngữ lập trình", 3),
            ("Cấu trúc dữ liệu", 4),
            ("Mạng máy tính", 5)
        };
        var created = new Dictionary<string, Topic>();
        foreach (var (name, sort) in roots)
        {
            var topic = new Topic { SubjectId = subject.Id, Name = name, SortOrder = sort, IsActive = true };
            await unitOfWork.Topics.AddAsync(topic, ct);
            created[name] = topic;
        }

        await unitOfWork.SaveChangesAsync(ct);
        var programming = created["Ngôn ngữ lập trình"];
        await unitOfWork.Topics.AddAsync(new Topic
        {
            SubjectId = subject.Id,
            ParentTopicId = programming.Id,
            Name = "Câu lệnh rẽ nhánh",
            SortOrder = 1,
            IsActive = true
        }, ct);
        await unitOfWork.Topics.AddAsync(new Topic
        {
            SubjectId = subject.Id,
            ParentTopicId = programming.Id,
            Name = "Vòng lặp",
            SortOrder = 2,
            IsActive = true
        }, ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    // Trường, năm học, lớp và vài hồ sơ mẫu để các bảng tổ chức không trống sau khi cài.
    private static async Task SeedSchoolSampleAsync(
        AiQuizDbContext db,
        IUnitOfWork unitOfWork,
        IPasswordHasher passwords,
        IConfiguration config,
        CancellationToken ct)
    {
        if (await db.Schools.AnyAsync(ct))
            return;

        var password = config["Seed:Password"];
        if (string.IsNullOrWhiteSpace(password))
            return;

        var now = DateTime.UtcNow;
        var school = new School
        {
            Code = "THCS-MAU",
            Name = "THCS mẫu",
            Address = "Nội bộ, dùng cho dữ liệu khởi tạo",
            IsActive = true,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await unitOfWork.Schools.AddAsync(school, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var year = new SchoolYear
        {
            SchoolId = school.Id,
            Name = "2026-2027",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 5, 31),
            IsCurrent = true,
            Status = SchoolYearStatus.Open
        };
        await unitOfWork.Schools.AddYearAsync(year, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var schoolAdmin = await CreateUserAsync(unitOfWork, passwords, "schooladmin", "Quản trị trường mẫu", password, RoleCodes.SchoolAdmin, now, ct);
        var teacher = await CreateUserAsync(unitOfWork, passwords, "teacher", "Giáo viên mẫu", password, RoleCodes.Teacher, now, ct);
        var studentOne = await CreateUserAsync(unitOfWork, passwords, "hs26001", "Nguyễn An", password, RoleCodes.Student, now, ct);
        var studentTwo = await CreateUserAsync(unitOfWork, passwords, "hs26002", "Trần Bình", password, RoleCodes.Student, now, ct);

        var grades = await unitOfWork.GradeLevels.ListAsync(ct);
        Classroom? homeroom = null;
        foreach (var grade in grades.OrderBy(item => item.SortOrder))
        {
            var room = new Classroom
            {
                SchoolId = school.Id,
                SchoolYearId = year.Id,
                GradeLevelId = grade.Id,
                ClassCode = grade.Code + "A1",
                Name = "Lớp " + grade.Code + "A1",
                Capacity = 40,
                Status = ClassroomStatus.Active,
                HomeroomTeacherUserId = grade.Code == "8" ? teacher.Id : null
            };
            await unitOfWork.Classrooms.AddAsync(room, ct);
            if (grade.Code == "8")
                homeroom = room;
        }

        await unitOfWork.SaveChangesAsync(ct);
        await unitOfWork.Schools.AddMembershipAsync(Member(school.Id, schoolAdmin.Id, SchoolRole.SchoolAdmin), ct);
        await unitOfWork.Schools.AddMembershipAsync(Member(school.Id, teacher.Id, SchoolRole.Teacher), ct);

        var subject = await unitOfWork.Subjects.FindByCodeAsync("TIN", ct);
        if (homeroom is not null && subject is not null)
        {
            await unitOfWork.Classrooms.AddTeacherAsync(new ClassMembership
            {
                ClassroomId = homeroom.Id,
                UserId = teacher.Id,
                ClassRole = ClassRole.SubjectTeacher,
                SubjectId = subject.Id
            }, ct);
            await EnrollAsync(unitOfWork, school.Id, homeroom.Id, studentOne, "HS26001", new DateOnly(2012, 3, 12), Gender.Male, ct);
            await EnrollAsync(unitOfWork, school.Id, homeroom.Id, studentTwo, "HS26002", new DateOnly(2012, 8, 2), Gender.Female, ct);
        }

        await unitOfWork.SaveChangesAsync(ct);
    }

    // Vài câu đã duyệt để ngân hàng câu hỏi và phương án có dữ liệu khởi tạo.
    private static async Task SeedQuestionsAsync(
        AiQuizDbContext db,
        IUnitOfWork unitOfWork,
        IConfiguration config,
        CancellationToken ct)
    {
        if (await db.Questions.AnyAsync(ct))
            return;
        var adminName = config["Seed:Username"] ?? "admin";
        var admin = await unitOfWork.Users.FindByUsernameAsync(adminName, ct);
        if (admin is null)
            return;
        var topics = await unitOfWork.Topics.ListAsync(null, ct);
        var byName = topics.ToDictionary(topic => topic.Name);
        if (!byName.ContainsKey("Thuật toán") || !byName.ContainsKey("Vòng lặp") || !byName.ContainsKey("Biểu diễn thông tin"))
            return;

        var now = DateTime.UtcNow;
        await unitOfWork.Questions.AddAsync(SampleQuestion(
            byName["Biểu diễn thông tin"].Id, admin.Id, now, 1, QuestionLevel.Easy,
            "1 byte bằng bao nhiêu bit?",
            [("4 bit", false), ("8 bit", true), ("16 bit", false), ("32 bit", false)]), ct);
        await unitOfWork.Questions.AddAsync(SampleQuestion(
            byName["Thuật toán"].Id, admin.Id, now, 2, QuestionLevel.Medium,
            "Thuật toán là gì?",
            [("Một dãy bước hữu hạn để giải quyết bài toán", true), ("Một ngôn ngữ lập trình", false), ("Một thiết bị phần cứng", false), ("Một hệ điều hành", false)]), ct);
        await unitOfWork.Questions.AddAsync(SampleQuestion(
            byName["Vòng lặp"].Id, admin.Id, now, 3, QuestionLevel.Medium,
            "Vòng lặp for thường dùng khi nào?",
            [("Khi biết trước số lần lặp", true), ("Khi không bao giờ cần dừng", false), ("Chỉ khi viết hàm đệ quy", false), ("Khi chương trình chỉ chạy một lệnh", false)]), ct);
        await unitOfWork.SaveChangesAsync(ct);
    }

    private static async Task<User> CreateUserAsync(
        IUnitOfWork unitOfWork,
        IPasswordHasher passwords,
        string username,
        string fullName,
        string password,
        string roleCode,
        DateTime now,
        CancellationToken ct)
    {
        var role = await unitOfWork.Roles.FindByCodeAsync(roleCode, ct)
            ?? throw new InvalidOperationException("Thiếu vai trò " + roleCode + ".");
        var user = new User
        {
            Username = username,
            FullName = fullName,
            PasswordHash = passwords.Hash(password),
            Status = UserStatus.Active,
            CreatedAtUtc = now,
            UserRoles = [new UserRole { RoleId = role.Id }]
        };
        await unitOfWork.Users.AddAsync(user, ct);
        await unitOfWork.SaveChangesAsync(ct);
        return user;
    }

    private static SchoolMembership Member(long schoolId, long userId, SchoolRole role) => new()
    {
        SchoolId = schoolId,
        UserId = userId,
        SchoolRole = role,
        Status = MembershipStatus.Active,
        StartDate = new DateOnly(2026, 9, 1)
    };

    private static async Task EnrollAsync(
        IUnitOfWork unitOfWork,
        long schoolId,
        long classroomId,
        User user,
        string code,
        DateOnly birth,
        Gender gender,
        CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var student = new StudentProfile
        {
            SchoolId = schoolId,
            StudentCode = code,
            FullName = user.FullName,
            DateOfBirth = birth,
            Gender = gender,
            UserId = user.Id,
            Status = StudentProfileStatus.Active,
            CreatedAtUtc = now,
            UpdatedAtUtc = now,
            Enrollments =
            [
                new StudentEnrollment
                {
                    ClassroomId = classroomId,
                    EnrollmentDate = new DateOnly(2026, 9, 5),
                    Status = EnrollmentStatus.Active,
                    CreatedAtUtc = now
                }
            ]
        };
        await unitOfWork.StudentRecords.AddAsync(student, ct);
    }

    private static Question SampleQuestion(
        long topicId,
        long authorId,
        DateTime now,
        byte difficultyId,
        QuestionLevel level,
        string content,
        (string Content, bool IsCorrect)[] options) => new()
    {
        TopicId = topicId,
        Content = content,
        Level = level,
        DifficultyLevelId = difficultyId,
        Type = QuestionType.SingleChoice,
        Status = QuestionStatus.Approved,
        Source = QuestionSource.Manual,
        CreatedById = authorId,
        ReviewedById = authorId,
        CreatedAtUtc = now,
        UpdatedAtUtc = now,
        ReviewedAtUtc = now,
        Options = options.Select((option, index) => new QuestionOption
        {
            Content = option.Content,
            IsCorrect = option.IsCorrect,
            SortOrder = index + 1
        }).ToList()
    };

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
