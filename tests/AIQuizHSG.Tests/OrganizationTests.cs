using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Organization;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Tests;

// Luật trường, lớp và phân lớp: trùng mã thì từ chối, chuyển lớp thì giữ lịch sử.
public class OrganizationTests
{
    private static readonly DateTime Now = new(2026, 10, 28, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task CreateSchool_RejectsDuplicateCode()
    {
        var store = new OrgStore();
        var service = Schools(store);
        var request = new SaveSchoolRequest { Code = "thcs-a", Name = "THCS A" };

        var first = await service.CreateAsync(request, Admin(), CancellationToken.None);
        var second = await service.CreateAsync(request, Admin(), CancellationToken.None);

        Assert.Equal(OperationStatus.Ok, first.Status);
        Assert.Equal(Messages.SchoolCodeExists, second.Message);
    }

    [Fact]
    public async Task CreateYear_RejectsDuplicateName_AndClassroomMustMatchSchool()
    {
        var store = new OrgStore();
        var schools = Schools(store);
        var rooms = Rooms(store);
        var school = (await schools.CreateAsync(new SaveSchoolRequest { Code = "A", Name = "A" }, Admin(), CancellationToken.None)).Value!;
        var other = (await schools.CreateAsync(new SaveSchoolRequest { Code = "B", Name = "B" }, Admin(), CancellationToken.None)).Value!;
        var yearRequest = new SaveSchoolYearRequest { Name = "2026-2027", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2027, 5, 31) };
        var year = (await schools.CreateYearAsync(school.Id, yearRequest, Admin(), CancellationToken.None)).Value!;
        var duplicate = await schools.CreateYearAsync(school.Id, yearRequest, Admin(), CancellationToken.None);
        store.Grades.Add(new GradeLevel { Id = 8, Code = "8", Name = "Khối 8", IsActive = true });

        var wrong = await rooms.CreateAsync(other.Id, new SaveClassroomRequest
        {
            SchoolYearId = year.Id,
            GradeLevelId = 8,
            ClassCode = "8A1",
            Name = "8A1"
        }, Admin(), CancellationToken.None);
        var created = await rooms.CreateAsync(school.Id, new SaveClassroomRequest
        {
            SchoolYearId = year.Id,
            GradeLevelId = 8,
            ClassCode = "8A1",
            Name = "8A1"
        }, Admin(), CancellationToken.None);
        var sameCode = await rooms.CreateAsync(school.Id, new SaveClassroomRequest
        {
            SchoolYearId = year.Id,
            GradeLevelId = 8,
            ClassCode = "8a1",
            Name = "Lớp khác"
        }, Admin(), CancellationToken.None);

        Assert.Equal(Messages.SchoolYearNameExists, duplicate.Message);
        Assert.Equal(Messages.SchoolYearWrongSchool, wrong.Message);
        Assert.Equal(OperationStatus.Ok, created.Status);
        Assert.Equal(Messages.ClassroomCodeExists, sameCode.Message);
    }

    [Fact]
    public async Task Enroll_RejectsOverlap_AndTransferKeepsHistory()
    {
        var store = new OrgStore();
        var schools = Schools(store);
        var rooms = Rooms(store);
        var records = Records(store);
        var school = (await schools.CreateAsync(new SaveSchoolRequest { Code = "A", Name = "A" }, Admin(), CancellationToken.None)).Value!;
        var year = (await schools.CreateYearAsync(school.Id, new SaveSchoolYearRequest
        {
            Name = "2026-2027",
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2027, 5, 31)
        }, Admin(), CancellationToken.None)).Value!;
        store.Grades.Add(new GradeLevel { Id = 8, Code = "8", Name = "Khối 8", IsActive = true });
        var first = (await rooms.CreateAsync(school.Id, Room(year.Id, "8A1"), Admin(), CancellationToken.None)).Value!;
        var second = (await rooms.CreateAsync(school.Id, Room(year.Id, "8A2"), Admin(), CancellationToken.None)).Value!;
        var student = (await records.CreateAsync(school.Id, new SaveStudentProfileRequest { StudentCode = "hs01", FullName = "Học sinh" }, Admin(), CancellationToken.None)).Value!;
        var duplicateStudent = await records.CreateAsync(school.Id, new SaveStudentProfileRequest { StudentCode = "HS01", FullName = "Trùng" }, Admin(), CancellationToken.None);

        var enrolled = await records.EnrollAsync(first.Id, new EnrollStudentRequest { StudentId = student.Id }, Admin(), CancellationToken.None);
        var overlap = await records.EnrollAsync(second.Id, new EnrollStudentRequest { StudentId = student.Id }, Admin(), CancellationToken.None);
        var moved = await records.TransferAsync(student.Id, new TransferStudentRequest { ClassroomId = second.Id }, Admin(), CancellationToken.None);

        Assert.Equal(Messages.StudentCodeExists, duplicateStudent.Message);
        Assert.Equal(OperationStatus.Ok, enrolled.Status);
        Assert.Equal(Messages.EnrollmentOverlap, overlap.Message);
        Assert.Equal(second.Id, moved.Value!.ClassroomId);
        Assert.Equal(2, store.Enrollments.Count);
        Assert.Equal(EnrollmentStatus.Transferred, store.Enrollments.Single(item => item.ClassroomId == first.Id).Status);
        Assert.Equal(EnrollmentStatus.Active, store.Enrollments.Single(item => item.ClassroomId == second.Id).Status);
    }

    [Fact]
    public async Task TeacherOfAnotherSchool_IsForbidden()
    {
        var store = new OrgStore();
        var schools = Schools(store);
        var school = (await schools.CreateAsync(new SaveSchoolRequest { Code = "A", Name = "A" }, Admin(), CancellationToken.None)).Value!;
        store.UsersData.Add(new User
        {
            Id = 2,
            Username = "gv",
            FullName = "Giáo viên",
            Status = UserStatus.Active,
            UserRoles = [new UserRole { Role = new Role { Code = RoleCodes.Teacher } }]
        });

        var result = await schools.GetAsync(school.Id, new Actor(2, null), CancellationToken.None);

        Assert.Equal(OperationStatus.Forbidden, result.Status);
    }

    private static SaveClassroomRequest Room(long yearId, string code) => new()
    {
        SchoolYearId = yearId,
        GradeLevelId = 8,
        ClassCode = code,
        Name = code
    };

    private static Actor Admin() => new(1, null);

    private static SchoolService Schools(OrgStore store) =>
        new(store, new FixedClock(), new SchoolAccess(store), new SaveSchoolRequestValidator(), new SaveSchoolYearRequestValidator(), new SaveGradeLevelRequestValidator());

    private static ClassroomService Rooms(OrgStore store) =>
        new(store, new SchoolAccess(store), new SaveClassroomRequestValidator());

    private static StudentRecordService Records(OrgStore store) =>
        new(store, new FixedClock(), new SchoolAccess(store), new SaveStudentProfileRequestValidator());

    private sealed class FixedClock : IClock
    {
        public DateTime UtcNow => Now;
    }

    private sealed class OrgStore : IUnitOfWork
    {
        public List<User> UsersData { get; } =
        [
            new User
            {
                Id = 1,
                Username = "admin",
                FullName = "Quản trị",
                Status = UserStatus.Active,
                UserRoles = [new UserRole { Role = new Role { Code = RoleCodes.Admin } }]
            }
        ];

        public List<School> SchoolRows { get; } = [];
        public List<SchoolYear> Years { get; } = [];
        public List<GradeLevel> Grades { get; } = [];
        public List<Classroom> Rooms { get; } = [];
        public List<StudentProfile> Students { get; } = [];
        public List<StudentEnrollment> Enrollments { get; } = [];
        public long NextId { get; set; } = 1;

        public OrgStore()
        {
            Schools = new SchoolRepo(this);
            GradeLevels = new GradeRepo(this);
            Classrooms = new RoomRepo(this);
            StudentRecords = new StudentRepo(this);
        }

        IUserRepository IUnitOfWork.Users => new UserRepo(this);
        public IRoleRepository Roles => throw new NotSupportedException();
        public ISubjectRepository Subjects => throw new NotSupportedException();
        public ITopicRepository Topics => throw new NotSupportedException();
        public IQuestionRepository Questions => throw new NotSupportedException();
        public IDifficultyLevelRepository DifficultyLevels => throw new NotSupportedException();
        public IExamProfileRepository ExamProfiles => throw new NotSupportedException();
        public ISchoolRepository Schools { get; }
        public IGradeLevelRepository GradeLevels { get; }
        public IClassroomRepository Classrooms { get; }
        public IStudentRecordRepository StudentRecords { get; }
        public IPermissionRepository Permissions { get; } = new PermissionRepo();
        public IExamRepository Exams => throw new NotSupportedException();
        public IAttemptRepository Attempts => throw new NotSupportedException();
        public IExamAssignmentRepository ExamAssignments => throw new NotSupportedException();
        public IOmrRepository Omr => throw new NotSupportedException();
        public IAuditLogRepository AuditLogs { get; } = new AuditRepo();

        public Task<int> SaveChangesAsync(CancellationToken ct)
        {
            foreach (var school in SchoolRows.Where(item => item.Id == 0))
                school.Id = NextId++;
            foreach (var year in Years.Where(item => item.Id == 0))
                year.Id = NextId++;
            foreach (var room in Rooms.Where(item => item.Id == 0))
                room.Id = NextId++;
            foreach (var student in Students.Where(item => item.Id == 0))
                student.Id = NextId++;
            foreach (var student in Students)
            {
                foreach (var enrollment in student.Enrollments.Where(item => item.Id == 0 && !Enrollments.Contains(item)))
                {
                    enrollment.StudentId = student.Id;
                    enrollment.Student = student;
                    Enrollments.Add(enrollment);
                }
            }

            foreach (var enrollment in Enrollments.Where(item => item.Id == 0))
                enrollment.Id = NextId++;

            return Task.FromResult(1);
        }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct) => action(ct);
        public Task<bool> CanConnectAsync(CancellationToken ct) => Task.FromResult(true);

        private sealed class UserRepo(OrgStore owner) : IUserRepository
        {
            public Task<User?> FindByIdAsync(long id, CancellationToken ct) =>
                Task.FromResult(owner.UsersData.FirstOrDefault(user => user.Id == id));
            public Task<bool> AnyAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<User?> FindByUsernameAsync(string username, CancellationToken ct) => throw new NotSupportedException();
            public Task<User?> FindForUpdateAsync(long id, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<User>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<User>> ListByRoleAsync(string roleCode, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<User>> ListAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task AddAsync(User user, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class PermissionRepo : IPermissionRepository
        {
            public Task<List<string>> ListCodesForUserAsync(long userId, CancellationToken ct) =>
                Task.FromResult(userId == 2 ? new List<string> { "school.view" } : []);
            public Task<bool> AnyAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<bool> AnyRoleLinkAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<List<Permission>> ListAsync(string? module, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<Permission>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct) => throw new NotSupportedException();
            public Task AddRangeAsync(IEnumerable<Permission> permissions, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<Permission>> ListForRoleAsync(long roleId, CancellationToken ct) => throw new NotSupportedException();
            public Task ReplaceRoleAsync(long roleId, IReadOnlyCollection<long> permissionIds, long? actorId, DateTime now, CancellationToken ct) => throw new NotSupportedException();
            public Task AddRoleLinksAsync(IEnumerable<RolePermission> links, CancellationToken ct) => throw new NotSupportedException();
            public Task AddMissingRoleLinksAsync(IEnumerable<RolePermission> links, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<UserPermission>> ListGrantsAsync(long userId, CancellationToken ct) => throw new NotSupportedException();
            public Task ReplaceUserGrantsAsync(long userId, IReadOnlyCollection<UserGrant> grants, long? actorId, DateTime now, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class SchoolRepo(OrgStore owner) : ISchoolRepository
        {
            public Task<School?> FindAsync(long id, CancellationToken ct) =>
                Task.FromResult(owner.SchoolRows.FirstOrDefault(school => school.Id == id));
            public Task<School?> FindByCodeAsync(string code, CancellationToken ct) =>
                Task.FromResult(owner.SchoolRows.FirstOrDefault(school => school.Code == code));
            public Task AddAsync(School school, CancellationToken ct)
            {
                owner.SchoolRows.Add(school);
                return Task.CompletedTask;
            }
            public Task<SchoolYear?> FindYearAsync(long id, CancellationToken ct) =>
                Task.FromResult(owner.Years.FirstOrDefault(year => year.Id == id));
            public Task<SchoolYear?> FindYearByNameAsync(long schoolId, string name, CancellationToken ct) =>
                Task.FromResult(owner.Years.FirstOrDefault(year => year.SchoolId == schoolId && year.Name == name));
            public Task AddYearAsync(SchoolYear year, CancellationToken ct)
            {
                owner.Years.Add(year);
                return Task.CompletedTask;
            }
            public Task ClearCurrentAsync(long schoolId, long? exceptYearId, CancellationToken ct) => Task.CompletedTask;
            public Task<bool> HasActiveMembershipAsync(long userId, long schoolId, CancellationToken ct) => Task.FromResult(false);
            public Task<List<School>> ListAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<List<SchoolYear>> ListYearsAsync(long schoolId, CancellationToken ct) => throw new NotSupportedException();
            public Task<bool> HasActiveRoleAsync(long userId, long schoolId, SchoolRole role, CancellationToken ct) => throw new NotSupportedException();
            public Task<SchoolMembership?> FindMembershipAsync(long schoolId, long userId, SchoolRole role, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<SchoolMembership>> ListMembershipsAsync(long schoolId, CancellationToken ct) => throw new NotSupportedException();
            public Task AddMembershipAsync(SchoolMembership membership, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class GradeRepo(OrgStore owner) : IGradeLevelRepository
        {
            public Task<GradeLevel?> FindAsync(long id, CancellationToken ct) =>
                Task.FromResult(owner.Grades.FirstOrDefault(level => level.Id == id));
            public Task<bool> AnyAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<List<GradeLevel>> ListAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<GradeLevel?> FindByCodeAsync(string code, CancellationToken ct) => throw new NotSupportedException();
            public Task AddAsync(GradeLevel level, CancellationToken ct) => throw new NotSupportedException();
            public Task AddRangeAsync(IEnumerable<GradeLevel> levels, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class RoomRepo(OrgStore owner) : IClassroomRepository
        {
            public Task<Classroom?> FindAsync(long id, CancellationToken ct) =>
                Task.FromResult(owner.Rooms.FirstOrDefault(room => room.Id == id));
            public Task<Classroom?> FindByCodeAsync(long schoolYearId, string classCode, CancellationToken ct) =>
                Task.FromResult(owner.Rooms.FirstOrDefault(room => room.SchoolYearId == schoolYearId && room.ClassCode == classCode));
            public Task AddAsync(Classroom classroom, CancellationToken ct)
            {
                owner.Rooms.Add(classroom);
                return Task.CompletedTask;
            }
            public Task<int> CountActiveStudentsAsync(long classroomId, CancellationToken ct) =>
                Task.FromResult(owner.Enrollments.Count(item => item.ClassroomId == classroomId && item.Status == EnrollmentStatus.Active && item.LeaveDate == null));
            public Task<List<Classroom>> ListAsync(long schoolId, long? schoolYearId, CancellationToken ct) => throw new NotSupportedException();
            public Task<bool> TeachesAsync(long classroomId, long userId, CancellationToken ct) => throw new NotSupportedException();
            public Task<ClassMembership?> FindSubjectTeacherAsync(long classroomId, long userId, long subjectId, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<ClassMembership>> ListTeachersAsync(long classroomId, CancellationToken ct) => throw new NotSupportedException();
            public Task AddTeacherAsync(ClassMembership membership, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class StudentRepo(OrgStore owner) : IStudentRecordRepository
        {
            public Task<StudentProfile?> FindAsync(long id, CancellationToken ct) =>
                Task.FromResult(owner.Students.FirstOrDefault(student => student.Id == id));
            public Task<StudentProfile?> FindByCodeAsync(long schoolId, string code, CancellationToken ct) =>
                Task.FromResult(owner.Students.FirstOrDefault(student => student.SchoolId == schoolId && student.StudentCode == code));
            public Task<List<StudentProfile>> FindAllByCodeAsync(string code, CancellationToken ct) =>
                Task.FromResult(owner.Students.Where(student => student.StudentCode == code).ToList());
            public Task AddAsync(StudentProfile student, CancellationToken ct)
            {
                owner.Students.Add(student);
                return Task.CompletedTask;
            }
            public Task<StudentEnrollment?> FindActiveEnrollmentAsync(long studentId, CancellationToken ct) =>
                Task.FromResult(owner.Enrollments.FirstOrDefault(item => item.StudentId == studentId && item.Status == EnrollmentStatus.Active && item.LeaveDate == null));
            public Task AddEnrollmentAsync(StudentEnrollment enrollment, CancellationToken ct)
            {
                owner.Enrollments.Add(enrollment);
                return Task.CompletedTask;
            }
            public Task<List<StudentProfile>> SearchAsync(long schoolId, string? keyword, CancellationToken ct) => throw new NotSupportedException();
            public Task<StudentProfile?> FindByUserAsync(long userId, CancellationToken ct) => Task.FromResult<StudentProfile?>(null);
            public Task<List<StudentProfile>> ListActiveInClassroomAsync(long classroomId, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class AuditRepo : IAuditLogRepository
        {
            public void Add(long? userId, string action, string entityName, long? entityId, string? details, string? ipAddress)
            {
            }
        }
    }
}
