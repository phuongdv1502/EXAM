using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Application.Omr;
using AIQuizHSG.Domain.Authorization;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AIQuizHSG.Infrastructure.Seeding;

// Dữ liệu khởi tạo chỉ thêm phần còn thiếu, không ghi đè bản ghi đã có.
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

        await SeedSubjectsAsync(unitOfWork, ct);

        await SeedDifficultyAsync(unitOfWork, config, clock: DateTime.UtcNow, ct);
        await SeedOrganizationAsync(unitOfWork, ct);
        await SeedTopicsAsync(unitOfWork, ct);
        await SeedSchoolSampleAsync(db, unitOfWork, passwords, config, ct);
        await SeedQuestionsAsync(db, unitOfWork, config, ct);
        await SeedFormStylesAsync(db, ct);
        await SeedOmrTemplatesAsync(unitOfWork, config, logger, ct);
        await SeedTeachingSubjectsAsync(db, ct);
        await SeedChartSeriesAsync(db, ct);
    }

    // Tám lần kiểm tra đã chấm, điểm tăng dần, để đường biểu đồ trên tổng quan có dữ liệu.
    private static async Task SeedChartSeriesAsync(AiQuizDbContext db, CancellationToken ct)
    {
        if (await db.Exams.AnyAsync(exam => exam.Title.StartsWith("Lần kiểm tra "), ct))
            return;

        var subject = await db.Subjects.FirstOrDefaultAsync(item => item.Code == "TIN", ct)
            ?? await db.Subjects.OrderBy(item => item.Id).FirstOrDefaultAsync(ct);
        var author = await db.Users.OrderBy(item => item.Id).Select(item => (long?)item.Id).FirstOrDefaultAsync(ct);
        var students = await db.UserRoles
            .Where(link => link.Role.Code == RoleCodes.Student)
            .Select(link => link.UserId)
            .Distinct()
            .OrderBy(id => id)
            .Take(2)
            .ToListAsync(ct);
        if (subject is null || author is null || students.Count == 0)
            return;

        var averages = new decimal[] { 42, 51, 57, 64, 60, 71, 79, 88 };
        var now = DateTime.UtcNow;
        for (var index = 0; index < averages.Length; index++)
        {
            var when = now.AddDays(-7 * (averages.Length - index));
            var exam = new Exam
            {
                Title = $"Lần kiểm tra {index + 1}",
                Description = "Dữ liệu mẫu để xem đường điểm trên tổng quan.",
                SubjectId = subject.Id,
                DurationMinutes = 45,
                MaxAttempts = 1,
                ShuffleQuestions = false,
                ShuffleOptions = false,
                PublishResults = true,
                Status = ExamStatus.Closed,
                CreatedById = author.Value,
                CreatedAtUtc = when,
                PublishedAtUtc = when
            };
            db.Exams.Add(exam);
            await db.SaveChangesAsync(ct);

            for (var studentIndex = 0; studentIndex < students.Count; studentIndex++)
            {
                var delta = studentIndex == 0 ? -4m : 4m;
                var score = Math.Clamp(averages[index] + delta, 0, 100);
                db.ExamAttempts.Add(new ExamAttempt
                {
                    ExamId = exam.Id,
                    UserId = students[studentIndex],
                    AttemptNumber = 1,
                    StartedAtUtc = when,
                    DueAtUtc = when.AddMinutes(45),
                    SubmittedAtUtc = when.AddMinutes(40),
                    Status = AttemptStatus.Graded,
                    Score = score,
                    MaxScore = 100
                });
            }
        }

        await db.SaveChangesAsync(ct);
    }

    // Bổ sung kiểu phiếu còn thiếu. Không ghi đè kiểu đã có.
    private static async Task SeedFormStylesAsync(AiQuizDbContext db, CancellationToken ct)
    {
        var codes = await db.OmrFormStyles.Select(item => item.Code).ToListAsync(ct);
        var missing = new List<OmrFormStyle>();
        void Add(string code, string name, string description, int sort)
        {
            if (!codes.Contains(code))
                missing.Add(new OmrFormStyle { Code = code, Name = name, Description = description, SortOrder = sort, IsActive = true });
        }

        Add("Khung", "Khung nhiều cột", "Chia câu thành vài cột, mỗi cột một khung.", 1);
        Add("MotCot", "Một cột", "Cả phiếu một cột, ô to, dễ tô.", 2);
        Add("Luoi", "Lưới ô", "Câu xếp ngang thành lưới, gọn trên một trang.", 3);
        Add("NhieuPhan", "Phiếu Bộ", "Phiếu Bộ Giáo dục: phần I bốn đáp án, phần II đúng/sai, phần III điền số.", 4);
        Add("PhieuThi", "Phiếu thi", "Phiếu trắc nghiệm có số báo danh, mã đề và chữ ký cán bộ coi thi. Số cột theo số câu.", 5);
        Add("KiemTra", "Phiếu kiểm tra", "Phiếu A4: phần I bốn đáp án theo cột 10 câu, phần II đúng/sai. Đặt số câu khi tạo mẫu.", 6);
        Add("TuLuan", "Phiếu có tự luận", "Phần trắc nghiệm, đúng/sai, điền số, ô điểm tự luận và phần giấy kẻ.", 7);
        if (missing.Count == 0)
            return;

        db.OmrFormStyles.AddRange(missing);
        await db.SaveChangesAsync(ct);
    }

    // Ba mẫu phiếu đang dùng. Chỉ thêm khi chưa có mã, không ghi đè tọa độ đã lưu.
    private static async Task SeedOmrTemplatesAsync(IUnitOfWork unitOfWork, IConfiguration config, ILogger logger, CancellationToken ct)
    {
        // Không khóa vào đúng tên admin. Production đổi tên đăng nhập thì vẫn gắn mẫu cho một quản trị.
        var username = config["Seed:Username"]?.Trim();
        var admin = string.IsNullOrWhiteSpace(username)
            ? null
            : await unitOfWork.Users.FindByUsernameAsync(username, ct);
        admin ??= (await unitOfWork.Users.ListByRoleAsync(RoleCodes.Admin, ct)).FirstOrDefault();
        if (admin is null)
        {
            logger.LogWarning("Chưa có tài khoản quản trị nên chưa thêm mẫu phiếu PHIEU-KIEM-TRA, PHIEU-BO, PHIEU-TU-LUAN.");
            return;
        }

        var now = DateTime.UtcNow;
        await Add(unitOfWork, admin.Id, now, "PHIEU-KIEM-TRA", () =>
            OmrTemplateConfig.TryFromCheck([("BonDapAn", 32), ("DungSai", 2)], out var layout) ? layout : null, ct);
        await Add(unitOfWork, admin.Id, now, "PHIEU-BO", () =>
            OmrTemplateConfig.TryFromParts([("BonDapAn", 40), ("DungSai", 8), ("DienSo", 6)], out var layout) ? layout : null, ct);
        await Add(unitOfWork, admin.Id, now, "PHIEU-TU-LUAN", () =>
            OmrTemplateConfig.TryFromEssay([("BonDapAn", 8), ("DungSai", 4), ("DienSo", 2)], out var layout) ? layout : null, ct);
        await unitOfWork.SaveChangesAsync(ct);

        async Task Add(IUnitOfWork unitOfWork, long userId, DateTime now, string code, Func<OmrTemplateConfig?> build, CancellationToken ct)
        {
            if (await unitOfWork.Omr.TemplateCodeExistsAsync(code, 1, ct))
                return;
            logger.LogInformation("Thêm mẫu phiếu {Code}.", code);
            var layout = build();
            if (layout is null)
                return;
            await unitOfWork.Omr.AddTemplateAsync(new OmrTemplate
            {
                Code = code,
                Version = 1,
                ConfigJson = layout.ToJson(),
                IsActive = true,
                CreatedAtUtc = now,
                CreatedById = userId
            }, ct);
        }
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

        var now = DateTime.UtcNow;
        var existingCodes = (await unitOfWork.Permissions.ListAsync(null, ct)).Select(item => item.Code).ToHashSet();
        var missingPermissions = PermissionCatalog.All.Where(item => !existingCodes.Contains(item.Code)).Select(item => new Permission
        {
            Code = item.Code,
            Name = item.Name,
            Module = item.Module,
            IsSystem = true,
            IsActive = true,
            CreatedAtUtc = now
        }).ToList();
        if (missingPermissions.Count > 0)
        {
            await unitOfWork.Permissions.AddRangeAsync(missingPermissions, ct);
            await unitOfWork.SaveChangesAsync(ct);
        }

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
                    CreatedAtUtc = now
                });
            }
        }

        await unitOfWork.Permissions.AddMissingRoleLinksAsync(links, ct);
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

    // Thêm môn chưa có theo mã. Môn đã tạo thì giữ nguyên tên và mô tả.
    private static async Task SeedSubjectsAsync(IUnitOfWork unitOfWork, CancellationToken ct)
    {
        var codes = (await unitOfWork.Subjects.ListAsync(ct)).Select(item => item.Code).ToHashSet();
        var added = false;
        foreach (var item in SampleBank.Subjects)
        {
            if (codes.Contains(item.Code))
                continue;
            await unitOfWork.Subjects.AddAsync(new Subject
            {
                Code = item.Code,
                Name = item.Name,
                Description = item.Description,
                IsActive = true
            }, ct);
            added = true;
        }

        if (added)
            await unitOfWork.SaveChangesAsync(ct);
    }

    // Thêm chuyên đề còn thiếu theo từng môn. Chuyên đề cha được lưu trước chuyên đề con.
    private static async Task SeedTopicsAsync(IUnitOfWork unitOfWork, CancellationToken ct)
    {
        var subjects = (await unitOfWork.Subjects.ListAsync(ct)).ToDictionary(item => item.Code);
        var known = (await unitOfWork.Topics.ListAsync(null, ct))
            .ToDictionary(item => (item.SubjectId, item.Name));
        var added = false;
        foreach (var item in SampleBank.Topics.Where(item => item.Parent is null))
        {
            if (!subjects.TryGetValue(item.SubjectCode, out var subject))
                continue;
            if (known.ContainsKey((subject.Id, item.Name)))
                continue;
            var topic = new Topic { SubjectId = subject.Id, Name = item.Name, SortOrder = item.Sort, IsActive = true };
            await unitOfWork.Topics.AddAsync(topic, ct);
            known[(subject.Id, item.Name)] = topic;
            added = true;
        }

        if (added)
            await unitOfWork.SaveChangesAsync(ct);

        added = false;
        foreach (var item in SampleBank.Topics.Where(item => item.Parent is not null))
        {
            if (!subjects.TryGetValue(item.SubjectCode, out var subject))
                continue;
            if (known.ContainsKey((subject.Id, item.Name)))
                continue;
            if (!known.TryGetValue((subject.Id, item.Parent!), out var parent))
                continue;
            var topic = new Topic
            {
                SubjectId = subject.Id,
                ParentTopicId = parent.Id,
                Name = item.Name,
                SortOrder = item.Sort,
                IsActive = true
            };
            await unitOfWork.Topics.AddAsync(topic, ct);
            known[(subject.Id, item.Name)] = topic;
            added = true;
        }

        if (added)
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

    // Giáo viên đã được giao môn ở lớp thì giữ môn đó trong danh sách được dạy.
    private static async Task SeedTeachingSubjectsAsync(AiQuizDbContext db, CancellationToken ct)
    {
        var links = await db.ClassMemberships.AsNoTracking()
            .Select(item => new { item.UserId, item.SubjectId })
            .Distinct()
            .ToListAsync(ct);
        var existing = await db.TeacherSubjects.AsNoTracking()
            .Select(item => new { item.UserId, item.SubjectId })
            .ToListAsync(ct);
        var have = existing.Select(item => (item.UserId, item.SubjectId)).ToHashSet();
        var missing = links.Where(item => !have.Contains((item.UserId, item.SubjectId))).ToList();
        if (missing.Count == 0)
            return;

        db.TeacherSubjects.AddRange(missing.Select(item => new TeacherSubject
        {
            UserId = item.UserId,
            SubjectId = item.SubjectId
        }));
        await db.SaveChangesAsync(ct);
    }

    // Thêm câu mẫu chưa có trong ngân hàng. Câu trùng nội dung thì bỏ qua.
    private static async Task SeedQuestionsAsync(
        AiQuizDbContext db,
        IUnitOfWork unitOfWork,
        IConfiguration config,
        CancellationToken ct)
    {
        var adminName = config["Seed:Username"] ?? "admin";
        var admin = await unitOfWork.Users.FindByUsernameAsync(adminName, ct);
        if (admin is null)
            return;

        var subjects = (await unitOfWork.Subjects.ListAsync(ct)).ToDictionary(item => item.Code);
        var topics = (await unitOfWork.Topics.ListAsync(null, ct)).ToDictionary(item => (item.SubjectId, item.Name));
        var existing = (await db.Questions.AsNoTracking().Select(item => item.Content).ToListAsync(ct)).ToHashSet();
        var now = DateTime.UtcNow;
        var added = 0;
        foreach (var item in SampleBank.Questions)
        {
            if (existing.Contains(item.Content))
                continue;
            if (!subjects.TryGetValue(item.SubjectCode, out var subject))
                continue;
            if (!topics.TryGetValue((subject.Id, item.Topic), out var topic))
                continue;
            await unitOfWork.Questions.AddAsync(SampleQuestion(
                topic.Id,
                admin.Id,
                now,
                item.Level,
                Rank(item.Level),
                item.Content,
                Options(item)), ct);
            existing.Add(item.Content);
            added++;
        }

        // Ý đúng/sai và câu điền số môn Tin học. Câu trùng nội dung thì bỏ qua.
        if (subjects.TryGetValue("TIN", out var tin))
        {
            foreach (var item in TrueFalseSamples)
            {
                if (existing.Contains(item.Content) || !topics.TryGetValue((tin.Id, item.Topic), out var topic))
                    continue;
                await unitOfWork.Questions.AddAsync(SampleQuestion(
                    topic.Id,
                    admin.Id,
                    now,
                    item.Level,
                    Rank(item.Level),
                    item.Content,
                    [("Đúng", item.IsTrue), ("Sai", !item.IsTrue)],
                    QuestionType.TrueFalse), ct);
                existing.Add(item.Content);
                added++;
            }

            foreach (var item in FillNumberSamples)
            {
                if (existing.Contains(item.Content) || !topics.TryGetValue((tin.Id, item.Topic), out var topic))
                    continue;
                await unitOfWork.Questions.AddAsync(SampleQuestion(
                    topic.Id,
                    admin.Id,
                    now,
                    item.Level,
                    Rank(item.Level),
                    item.Content,
                    [(item.Answer, true), (item.Wrong, false)],
                    QuestionType.FillNumber), ct);
                existing.Add(item.Content);
                added++;
            }
        }

        if (added > 0)
            await unitOfWork.SaveChangesAsync(ct);
    }

    private static QuestionLevel Rank(byte level) => level switch
    {
        1 => QuestionLevel.Easy,
        2 or 3 => QuestionLevel.Medium,
        _ => QuestionLevel.Hard
    };

    private static (string Content, bool IsCorrect)[] Options(QuestionSeed item) =>
    [
        (item.A, item.Correct == 0),
        (item.B, item.Correct == 1),
        (item.C, item.Correct == 2),
        (item.D, item.Correct == 3)
    ];

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

    private static readonly (string Topic, byte Level, string Content, bool IsTrue)[] TrueFalseSamples =
    [
        ("Thuật toán", 2, "Tìm kiếm nhị phân chỉ áp dụng trên dãy đã sắp xếp.", true),
        ("Thuật toán", 2, "Thuật toán luôn kết thúc sau một số bước hữu hạn.", true),
        ("Thuật toán", 2, "Sơ đồ khối dùng hình thoi để biểu diễn bước nhập dữ liệu.", false),
        ("Thuật toán", 2, "Một thuật toán có thể có nhiều cách cài đặt khác nhau.", true),
        ("Biểu diễn thông tin", 2, "Bit là đơn vị thông tin nhỏ nhất.", true),
        ("Biểu diễn thông tin", 2, "RAM vẫn giữ dữ liệu khi tắt nguồn.", false),
        ("Biểu diễn thông tin", 2, "1 KB bằng 1024 byte.", true),
        ("Cấu trúc dữ liệu", 2, "Ngăn xếp lấy phần tử theo kiểu vào sau ra trước.", true),
        ("Cấu trúc dữ liệu", 2, "Hàng đợi lấy phần tử theo kiểu vào sau ra trước.", false),
        ("Cấu trúc dữ liệu", 3, "Mảng n phần tử, chỉ số bắt đầu từ 0, thì chỉ số cuối là n.", false),
        ("Vòng lặp", 2, "Vòng lặp for thường dùng khi đã biết số lần lặp.", true),
        ("Ngôn ngữ lập trình", 2, "Một biến có thể đổi giá trị trong khi chương trình chạy.", true),
        ("Thuật toán", 3, "Tìm kiếm nhị phân luôn nhanh hơn tìm tuyến tính trên dãy chưa sắp xếp.", false),
        ("Thuật toán", 3, "Hình chữ nhật trong sơ đồ khối biểu diễn điều kiện rẽ nhánh.", false),
        ("Mạng máy tính", 2, "Địa chỉ IP dùng để định danh thiết bị trên mạng.", true),
        ("Mạng máy tính", 3, "HTTP thường dùng để xem trang web.", true),
        ("Thuật toán", 1, "Sắp xếp nổi bọt so sánh các cặp phần tử kề nhau.", true),
        ("Thuật toán", 2, "Tìm tuyến tính chỉ chạy đúng khi dãy đã được sắp xếp.", false),
        ("Thuật toán", 2, "Một lượt sắp xếp chọn đưa phần tử nhỏ nhất còn lại vào vị trí kế tiếp.", true),
        ("Thuật toán", 3, "Đệ quy tính giai thừa vẫn đúng khi không có điều kiện dừng.", false),
        ("Thuật toán", 4, "Tìm kiếm nhị phân trên dãy 16 phần tử cần nhiều nhất 4 lần chia đôi.", true),
        ("Thuật toán", 3, "Số cách chọn 2 phần tử khác nhau từ 4 phần tử là 8.", false),
        ("Biểu diễn thông tin", 1, "Hệ nhị phân chỉ dùng hai chữ số 0 và 1.", true),
        ("Biểu diễn thông tin", 2, "Tệp có đuôi .png thường chứa văn bản thuần.", false),
        ("Biểu diễn thông tin", 3, "Số thập phân 15 viết ở hệ nhị phân là 1111.", true),
        ("Biểu diễn thông tin", 2, "1 MB bằng đúng 1000 byte.", false),
        ("Cấu trúc dữ liệu", 2, "Thêm một nút vào đầu danh sách liên kết không cần duyệt đến nút cuối.", true),
        ("Cấu trúc dữ liệu", 3, "Chèn một phần tử vào giữa mảng luôn chỉ mất một bước, không phụ thuộc độ dài mảng.", false),
        ("Cấu trúc dữ liệu", 2, "Hàng đợi lấy phần tử theo kiểu vào trước ra trước.", true),
        ("Cấu trúc dữ liệu", 4, "Ngăn xếp lấy phần tử vào trước ra trước.", false),
        ("Vòng lặp", 2, "Vòng lặp while có thể không chạy lần nào nếu điều kiện đã sai từ đầu.", true),
        ("Vòng lặp", 3, "Vòng ngoài chạy 3 lần, mỗi lần vòng trong chạy 4 lần, thì thân vòng trong chạy 7 lần.", false),
        ("Vòng lặp", 1, "Vòng for từ 1 đến 4 thực hiện đúng 4 lần.", true),
        ("Ngôn ngữ lập trình", 2, "Lệnh a ← a + 1 làm giá trị của a tăng thêm 1.", true),
        ("Ngôn ngữ lập trình", 3, "Biến số nguyên có thể gán trực tiếp bằng một xâu ký tự.", false),
        ("Câu lệnh rẽ nhánh", 2, "Khi điều kiện sai và có nhánh else, chương trình chạy nhánh else.", true),
        ("Câu lệnh rẽ nhánh", 3, "Với x = 7, điều kiện x > 10 có giá trị đúng.", false),
        ("Mạng máy tính", 1, "Bàn phím là thiết bị dùng để nhập văn bản.", true),
        ("Mạng máy tính", 3, "Địa chỉ 192.168.1.1 là một địa chỉ email.", false),
        ("Thuật toán", 1, "Thuật toán là một dãy các bước để giải một bài toán.", true),
        ("Biểu diễn thông tin", 1, "Máy tính lưu dữ liệu bằng các bit 0 và 1.", true),
        ("Ngôn ngữ lập trình", 1, "Chương trình máy tính được viết bằng một ngôn ngữ lập trình.", true),
        ("Cấu trúc dữ liệu", 1, "Mảng dùng để lưu nhiều giá trị cùng một kiểu.", true),
        ("Mạng máy tính", 1, "Chuột là thiết bị dùng để nhập thao tác.", true),
        ("Câu lệnh rẽ nhánh", 1, "Lệnh if dùng khi cần kiểm tra một điều kiện.", true),
        ("Vòng lặp", 1, "Vòng lặp dùng để làm một việc nhiều lần.", true),
        ("Biểu diễn thông tin", 1, "Màn hình là thiết bị dùng để gõ văn bản vào máy.", false),
        ("Thuật toán", 2, "Hình bình hành trong sơ đồ khối biểu diễn bước nhập hoặc xuất.", true),
        ("Biểu diễn thông tin", 2, "Bộ nhớ ROM mất hết dữ liệu ngay khi tắt nguồn.", false),
        ("Ngôn ngữ lập trình", 2, "Tên biến được phép bắt đầu bằng một chữ số.", false),
        ("Cấu trúc dữ liệu", 2, "Phần tử đầu của mảng đánh số từ 0 có chỉ số bằng 0.", true),
        ("Mạng máy tính", 2, "HTTPS là HTTP được bổ sung lớp bảo mật.", true),
        ("Câu lệnh rẽ nhánh", 2, "Nhánh if chỉ được chạy khi điều kiện đúng.", true),
        ("Vòng lặp", 2, "Vòng lặp vô hạn là vòng không có cách nào để dừng.", true),
        ("Thuật toán", 2, "Sắp xếp chọn hoán đổi hai phần tử kề nhau ở mỗi bước so sánh.", false),
        ("Thuật toán", 3, "Sau khi sắp tăng dần dãy 3, 1, 2 thì phần tử đứng đầu là 1.", true),
        ("Biểu diễn thông tin", 3, "Số nhị phân 101 bằng 6 ở hệ thập phân.", false),
        ("Ngôn ngữ lập trình", 3, "Cho a bằng 2 và b bằng 3. Sau lệnh a ← b thì a vẫn bằng 2.", false),
        ("Cấu trúc dữ liệu", 3, "Ngăn xếp có đỉnh là 3, bên dưới là 2 rồi 1. Lấy ra hai lần thì lần thứ hai được số 2.", true),
        ("Mạng máy tính", 3, "Địa chỉ 256.1.1.1 là một địa chỉ IPv4 hợp lệ.", false),
        ("Câu lệnh rẽ nhánh", 3, "Với n bằng 4, điều kiện n chia hết cho 2 có giá trị đúng.", true),
        ("Vòng lặp", 3, "Biến i đi từ 0 đến khi còn nhỏ hơn 5 thì vòng chạy 5 lần.", true),
        ("Thuật toán", 3, "Ước chung lớn nhất của 8 và 12 bằng 2.", false),
        ("Thuật toán", 4, "Tìm kiếm nhị phân trên dãy 32 phần tử đã sắp cần nhiều nhất 5 lần chia đôi.", true),
        ("Thuật toán", 4, "Sắp xếp nổi bọt có thể dừng sớm nếu một lượt không còn hoán đổi nào.", true),
        ("Biểu diễn thông tin", 4, "Số 255 cần đúng 8 bit khi viết ở hệ nhị phân, không kể bit dấu.", true),
        ("Cấu trúc dữ liệu", 4, "Hàng đợi hai đầu cho phép lấy phần tử ở cả hai đầu.", true),
        ("Ngôn ngữ lập trình", 4, "Phép chia nguyên 7 cho 2 cho kết quả 3.", true),
        ("Vòng lặp", 4, "Hai vòng lồng nhau, mỗi vòng chạy n lần, thì thân vòng trong chạy n mũ 2 lần.", true),
        ("Câu lệnh rẽ nhánh", 4, "Điều kiện x lớn hơn 0 và x nhỏ hơn 0 có thể cùng đúng.", false),
        ("Mạng máy tính", 4, "Mặt nạ 255.255.255.0 dành 24 bit cho phần mạng.", true),
        ("Thuật toán", 5, "Sắp xếp chọn trên n phần tử luôn so sánh đúng n nhân (n trừ 1) chia 2 lần.", true),
        ("Thuật toán", 5, "Tìm kiếm nhị phân trên dãy chưa sắp vẫn luôn tìm đúng vị trí nếu phần tử có trong dãy.", false),
        ("Thuật toán", 5, "Hàng đợi ưu tiên luôn lấy phần tử vào sớm nhất, không xét mức ưu tiên.", false),
        ("Biểu diễn thông tin", 5, "Số nguyên 16 bit có dấu biểu diễn được giá trị 40000.", false),
        ("Cấu trúc dữ liệu", 5, "Lấy phần tử cuối của danh sách liên kết đơn cần đi qua số nút tỉ lệ với độ dài danh sách.", true),
        ("Ngôn ngữ lập trình", 5, "Ba lệnh a ← a + b, b ← a - b, a ← a - b hoán đổi a và b khi không bị tràn số.", true),
        ("Vòng lặp", 5, "Đệ quy Fibonacci không lưu kết quả sẽ tính lại cùng một giá trị nhiều lần.", true),
        ("Mạng máy tính", 5, "Gói tin qua bộ định tuyến có thể đổi địa chỉ MAC nhưng vẫn giữ địa chỉ IP đích.", true)
    ];

    private static readonly (string Topic, byte Level, string Content, string Answer, string Wrong)[] FillNumberSamples =
    [
        ("Biểu diễn thông tin", 2, "Một byte gồm bao nhiêu bit?", "8", "4"),
        ("Biểu diễn thông tin", 2, "1010 ở hệ nhị phân bằng bao nhiêu ở hệ thập phân?", "10", "12"),
        ("Biểu diễn thông tin", 3, "Dãy bit độ dài 8 có bao nhiêu giá trị khác nhau?", "256", "128"),
        ("Biểu diễn thông tin", 2, "1 KB gồm bao nhiêu byte?", "1024", "1000"),
        ("Thuật toán", 2, "Tổng các số nguyên từ 1 đến 4 bằng bao nhiêu?", "10", "9"),
        ("Thuật toán", 2, "Phép chia 17 cho 5 cho số dư bao nhiêu?", "2", "3"),
        ("Thuật toán", 3, "ƯCLN của 12 và 18 bằng bao nhiêu?", "6", "3"),
        ("Cấu trúc dữ liệu", 2, "Chỉ số cuối của mảng 6 phần tử, đánh số từ 0, là bao nhiêu?", "5", "6"),
        ("Ngôn ngữ lập trình", 2, "Xâu HSG có bao nhiêu ký tự?", "3", "4"),
        ("Ngôn ngữ lập trình", 3, "Kết quả của (2 + 3) nhân 4 là bao nhiêu?", "20", "24"),
        ("Biểu diễn thông tin", 2, "Nhị phân 1111 bằng bao nhiêu ở hệ thập phân?", "15", "13"),
        ("Biểu diễn thông tin", 3, "2 mũ 4 bằng bao nhiêu?", "16", "8"),
        ("Biểu diễn thông tin", 1, "Một nibble gồm bao nhiêu bit?", "4", "8"),
        ("Thuật toán", 2, "Số cách chọn 2 phần tử khác nhau từ 4 phần tử là bao nhiêu?", "6", "8"),
        ("Thuật toán", 3, "Số hạng tiếp theo của dãy 2, 4, 8, 16 là bao nhiêu?", "32", "18"),
        ("Thuật toán", 4, "Tìm nhị phân trên 16 phần tử cần nhiều nhất bao nhiêu lần chia đôi?", "4", "8"),
        ("Cấu trúc dữ liệu", 2, "Mảng 8 phần tử đánh số từ 0. Chỉ số cuối là bao nhiêu?", "7", "8"),
        ("Ngôn ngữ lập trình", 1, "Cho a bằng 5. Sau lệnh a ← a + 1 thì a bằng bao nhiêu?", "6", "5"),
        ("Ngôn ngữ lập trình", 3, "Cho a bằng 3. Sau lệnh a ← a * 2 thì a bằng bao nhiêu?", "6", "5"),
        ("Vòng lặp", 2, "Vòng for i từ 1 đến 4 thực hiện bao nhiêu lần?", "4", "5"),
        ("Vòng lặp", 3, "s bắt đầu bằng 0 rồi cộng lần lượt 1, 2 và 3. s bằng bao nhiêu?", "6", "3"),
        ("Câu lệnh rẽ nhánh", 2, "x bằng 7. Hiệu x - 10 bằng bao nhiêu?", "-3", "3"),
        ("Biểu diễn thông tin", 1, "Hệ đếm nhị phân dùng bao nhiêu chữ số?", "2", "10"),
        ("Biểu diễn thông tin", 1, "Một bit nhận được bao nhiêu giá trị khác nhau?", "2", "1"),
        ("Ngôn ngữ lập trình", 1, "Cho a bằng 0. Sau lệnh a ← 1 thì a bằng bao nhiêu?", "1", "0"),
        ("Cấu trúc dữ liệu", 1, "Mảng lưu ba giá trị. Số phần tử của mảng là bao nhiêu?", "3", "2"),
        ("Vòng lặp", 1, "Một vòng chỉ thực hiện đúng một lần. Số lần chạy là bao nhiêu?", "1", "0"),
        ("Câu lệnh rẽ nhánh", 1, "Biến x đang giữ giá trị 5. x bằng bao nhiêu?", "5", "4"),
        ("Thuật toán", 1, "Tổng 2 cộng 3 bằng bao nhiêu?", "5", "6"),
        ("Mạng máy tính", 1, "Một địa chỉ IPv4 gồm bao nhiêu nhóm số?", "4", "6"),
        ("Biểu diễn thông tin", 2, "Nhị phân 100 bằng bao nhiêu ở hệ thập phân?", "4", "8"),
        ("Biểu diễn thông tin", 2, "1 MB gồm bao nhiêu KB?", "1024", "1000"),
        ("Thuật toán", 2, "Tổng của ba số 2 bằng bao nhiêu?", "6", "8"),
        ("Thuật toán", 2, "Số dư khi chia 20 cho 6 là bao nhiêu?", "2", "3"),
        ("Cấu trúc dữ liệu", 2, "Mảng 10 phần tử đánh số từ 0. Chỉ số của phần tử thứ ba là bao nhiêu?", "2", "3"),
        ("Ngôn ngữ lập trình", 2, "Xâu TINHOC có bao nhiêu ký tự?", "6", "5"),
        ("Vòng lặp", 2, "Biến i đi từ 0 đến 3. Vòng chạy bao nhiêu lần?", "4", "3"),
        ("Câu lệnh rẽ nhánh", 2, "n bằng 9. Phần nguyên của n chia cho 2 là bao nhiêu?", "4", "5"),
        ("Thuật toán", 3, "Bội chung nhỏ nhất của 4 và 6 bằng bao nhiêu?", "12", "24"),
        ("Thuật toán", 3, "Ước chung lớn nhất của 15 và 25 bằng bao nhiêu?", "5", "10"),
        ("Biểu diễn thông tin", 3, "2 mũ 6 bằng bao nhiêu?", "64", "32"),
        ("Biểu diễn thông tin", 3, "Nhị phân 1101 bằng bao nhiêu ở hệ thập phân?", "13", "11"),
        ("Ngôn ngữ lập trình", 3, "Cho a bằng 8. Sau phép chia nguyên a cho 2 thì a bằng bao nhiêu?", "4", "2"),
        ("Cấu trúc dữ liệu", 3, "Ngăn xếp rỗng, đẩy 4 rồi đẩy 9, sau đó lấy ra một phần tử. Giá trị lấy ra là bao nhiêu?", "9", "4"),
        ("Vòng lặp", 3, "s bằng 1, nhân với 2 rồi nhân với 3. s bằng bao nhiêu?", "6", "5"),
        ("Câu lệnh rẽ nhánh", 3, "x bằng 12. Nếu x chẵn thì lấy x chia 2, ngược lại giữ x. Kết quả là bao nhiêu?", "6", "12"),
        ("Thuật toán", 4, "Tìm tuyến tính trên 9 phần tử, trường hợp xấu nhất cần bao nhiêu lần so sánh?", "9", "4"),
        ("Thuật toán", 4, "Số lần chia đôi nhiều nhất khi tìm nhị phân trên 32 phần tử là bao nhiêu?", "5", "4"),
        ("Biểu diễn thông tin", 4, "Số nguyên không dấu 8 bit lớn nhất bằng bao nhiêu?", "255", "256"),
        ("Biểu diễn thông tin", 4, "Nhị phân 1010 cộng 0011 bằng bao nhiêu ở hệ thập phân?", "13", "14"),
        ("Cấu trúc dữ liệu", 4, "Mảng 100 phần tử đánh số từ 1. Chỉ số cuối cùng là bao nhiêu?", "100", "99"),
        ("Ngôn ngữ lập trình", 4, "2 mũ 3 cộng 2 mũ 2 bằng bao nhiêu?", "12", "10"),
        ("Vòng lặp", 4, "Vòng ngoài chạy 5 lần, mỗi lần vòng trong chạy 5 lần. Thân trong chạy bao nhiêu lần?", "25", "10"),
        ("Câu lệnh rẽ nhánh", 4, "x bằng -4. Trị tuyệt đối của x là bao nhiêu?", "4", "-4"),
        ("Thuật toán", 5, "Sắp xếp chọn với 5 phần tử thực hiện bao nhiêu phép so sánh?", "10", "5"),
        ("Thuật toán", 5, "Số cách xếp 3 bạn khác nhau thành một hàng là bao nhiêu?", "6", "3"),
        ("Biểu diễn thông tin", 5, "Số nguyên 8 bit có dấu nhỏ nhất bằng bao nhiêu?", "-128", "-127"),
        ("Biểu diễn thông tin", 5, "Một địa chỉ IPv4 gồm bao nhiêu bit?", "32", "16"),
        ("Cấu trúc dữ liệu", 5, "Cây nhị phân đầy đủ có 7 nút thì có bao nhiêu lá?", "4", "3"),
        ("Ngôn ngữ lập trình", 5, "a bằng 7, b bằng 2. Sau khi hoán đổi bằng cộng trừ thì a bằng bao nhiêu?", "2", "7"),
        ("Vòng lặp", 5, "Dãy Fibonacci bắt đầu 1, 1. Số hạng thứ 6 bằng bao nhiêu?", "8", "13"),
        ("Mạng máy tính", 5, "Mạng có tiền tố /30 còn bao nhiêu địa chỉ gán được cho máy, sau khi trừ địa chỉ mạng và địa chỉ quảng bá?", "2", "4")
    ];

    private static Question SampleQuestion(
        long topicId,
        long authorId,
        DateTime now,
        byte difficultyId,
        QuestionLevel level,
        string content,
        (string Content, bool IsCorrect)[] options,
        QuestionType type = QuestionType.SingleChoice) => new()
    {
        TopicId = topicId,
        Content = content,
        Level = level,
        DifficultyLevelId = difficultyId,
        Type = type,
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
