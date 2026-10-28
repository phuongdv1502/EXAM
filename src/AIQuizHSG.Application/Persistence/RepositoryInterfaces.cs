using AIQuizHSG.Application.Exams;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

// Tìm và thêm tài khoản. Không tự lưu; Unit of Work mới gọi SaveChanges.
public interface IUserRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<User?> FindByUsernameAsync(string username, CancellationToken ct);
    Task<User?> FindByIdAsync(long id, CancellationToken ct);
    Task<User?> FindForUpdateAsync(long id, CancellationToken ct);
    Task<List<User>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct);
    Task<List<User>> ListByRoleAsync(string roleCode, CancellationToken ct);
    Task AddAsync(User user, CancellationToken ct);
}

// Vai trò ADMIN, TEACHER, STUDENT. Dùng khi seed và khi gán cho tài khoản.
public interface IRoleRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<Role?> FindByCodeAsync(string code, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Role> roles, CancellationToken ct);
    Task<List<Role>> ListAsync(CancellationToken ct);
    Task<Role?> FindAsync(long id, CancellationToken ct);
}

// Môn học. Bản ghi cần sửa thì được theo dõi, tra mã khi nhập Excel thì không.
public interface ISubjectRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<List<Subject>> ListAsync(CancellationToken ct);
    Task<Subject?> FindAsync(long id, CancellationToken ct);
    Task<Subject?> FindByCodeAsync(string code, CancellationToken ct);
    Task AddAsync(Subject subject, CancellationToken ct);
}

// Chuyên đề, có thể lọc theo môn và đi lên chuyên đề cha.
public interface ITopicRepository
{
    Task<List<Topic>> ListAsync(long? subjectId, CancellationToken ct);
    Task<Topic?> FindAsync(long id, CancellationToken ct);
    Task AddAsync(Topic topic, CancellationToken ct);
}

// Câu hỏi và phương án. Thay phương án thì xóa bản cũ trước khi gắn bản mới.
public interface IQuestionRepository
{
    Task<Question?> FindAsync(long id, CancellationToken ct);
    Task<(int Total, List<Question> Items)> SearchAsync(QuestionFilter filter, CancellationToken ct);
    Task<List<Question>> ListWithOptionsAsync(IReadOnlyCollection<long> ids, CancellationToken ct);
    Task<List<Question>> ListEligibleAsync(long subjectId, IReadOnlyCollection<long> topicIds, QuestionType? type, CancellationToken ct);
    Task AddAsync(Question question, CancellationToken ct);
    void ReplaceOptions(Question question, IReadOnlyList<QuestionOption> options);
}

// Đề thi, câu đã chụp và danh sách thí sinh.
public interface IExamRepository
{
    Task<List<ExamSummary>> ListAsync(CancellationToken ct);
    Task<List<ExamSummary>> ListOpenForStudentAsync(long userId, CancellationToken ct);
    Task<Exam?> FindAsync(long id, CancellationToken ct);
    Task AddAsync(Exam exam, CancellationToken ct);
    void ReplaceQuestions(Exam exam, IReadOnlyList<ExamQuestion> questions);
    void ReplaceParticipants(Exam exam, IReadOnlyList<long> userIds);
    Task<Exam?> FindByGenerationKeyAsync(string key, CancellationToken ct);
}

// Năm mức độ câu hỏi. Không tự gán cho câu cũ.
public interface IDifficultyLevelRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<List<DifficultyLevel>> ListActiveAsync(CancellationToken ct);
    Task<DifficultyLevel?> FindAsync(byte id, CancellationToken ct);
    Task<DifficultyLevel?> FindByCodeAsync(string code, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<DifficultyLevel> levels, CancellationToken ct);
}

// Profile độ khó của đề và cơ cấu từng mức.
public interface IExamProfileRepository
{
    Task<bool> AnySystemDefaultAsync(CancellationToken ct);
    Task<List<ExamDifficultyProfile>> ListAsync(CancellationToken ct);
    Task<ExamDifficultyProfile?> FindAsync(long id, CancellationToken ct);
    Task<bool> IsUsedAsync(long id, CancellationToken ct);
    Task AddAsync(ExamDifficultyProfile profile, CancellationToken ct);
    void ReplaceLevels(ExamDifficultyProfile profile, IReadOnlyList<ExamDifficultyProfileLevel> levels);
}

// Lượt làm bài và phương án đã chọn. Không tính điểm.
public interface IAttemptRepository
{
    Task<int> CountAsync(long examId, long userId, CancellationToken ct);
    Task<ExamAttempt?> FindInProgressAsync(long examId, long userId, CancellationToken ct);
    Task<ExamAttempt?> FindAsync(long id, CancellationToken ct);
    Task<List<ExamAttempt>> ListByUserAsync(long userId, CancellationToken ct);
    Task<List<ExamAttempt>> ListByUserIdsAsync(IReadOnlyCollection<long> userIds, CancellationToken ct);
    Task AddAsync(ExamAttempt attempt, CancellationToken ct);
    void ReplaceSelections(AttemptAnswer answer, IReadOnlyList<long> optionIds);
}

// Chỉ đưa dòng nhật ký vào bộ nhớ theo dõi. Lưu cùng transaction với thao tác chính.
public interface IAuditLogRepository
{
    void Add(long? userId, string action, string entityName, long? entityId, string? details, string? ipAddress);
}

// Trường, năm học và thành viên trường.
public interface ISchoolRepository
{
    Task<List<School>> ListAsync(CancellationToken ct);
    Task<School?> FindAsync(long id, CancellationToken ct);
    Task<School?> FindByCodeAsync(string code, CancellationToken ct);
    Task AddAsync(School school, CancellationToken ct);
    Task<List<SchoolYear>> ListYearsAsync(long schoolId, CancellationToken ct);
    Task<SchoolYear?> FindYearAsync(long id, CancellationToken ct);
    Task<SchoolYear?> FindYearByNameAsync(long schoolId, string name, CancellationToken ct);
    Task AddYearAsync(SchoolYear year, CancellationToken ct);
    Task ClearCurrentAsync(long schoolId, long? exceptYearId, CancellationToken ct);
    Task<bool> HasActiveMembershipAsync(long userId, long schoolId, CancellationToken ct);
    Task<bool> HasActiveRoleAsync(long userId, long schoolId, SchoolRole role, CancellationToken ct);
    Task<SchoolMembership?> FindMembershipAsync(long schoolId, long userId, SchoolRole role, CancellationToken ct);
    Task<List<SchoolMembership>> ListMembershipsAsync(long schoolId, CancellationToken ct);
    Task AddMembershipAsync(SchoolMembership membership, CancellationToken ct);
}

// Danh mục khối lớp dùng chung.
public interface IGradeLevelRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<List<GradeLevel>> ListAsync(CancellationToken ct);
    Task<GradeLevel?> FindAsync(long id, CancellationToken ct);
    Task<GradeLevel?> FindByCodeAsync(string code, CancellationToken ct);
    Task AddAsync(GradeLevel level, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<GradeLevel> levels, CancellationToken ct);
}

// Lớp học và phân công giáo viên bộ môn.
public interface IClassroomRepository
{
    Task<List<Classroom>> ListAsync(long schoolId, long? schoolYearId, CancellationToken ct);
    Task<Classroom?> FindAsync(long id, CancellationToken ct);
    Task<Classroom?> FindByCodeAsync(long schoolYearId, string classCode, CancellationToken ct);
    Task AddAsync(Classroom classroom, CancellationToken ct);
    Task<int> CountActiveStudentsAsync(long classroomId, CancellationToken ct);
    Task<bool> TeachesAsync(long classroomId, long userId, CancellationToken ct);
    Task<ClassMembership?> FindSubjectTeacherAsync(long classroomId, long userId, long subjectId, CancellationToken ct);
    Task<List<ClassMembership>> ListTeachersAsync(long classroomId, CancellationToken ct);
    Task AddTeacherAsync(ClassMembership membership, CancellationToken ct);
}

// Hồ sơ học sinh và lịch sử phân lớp.
public interface IStudentRecordRepository
{
    Task<List<StudentProfile>> SearchAsync(long schoolId, string? keyword, CancellationToken ct);
    Task<StudentProfile?> FindAsync(long id, CancellationToken ct);
    Task<StudentProfile?> FindByCodeAsync(long schoolId, string code, CancellationToken ct);
    Task<StudentProfile?> FindByUserAsync(long userId, CancellationToken ct);
    Task AddAsync(StudentProfile student, CancellationToken ct);
    Task<StudentEnrollment?> FindActiveEnrollmentAsync(long studentId, CancellationToken ct);
    Task<List<StudentProfile>> ListActiveInClassroomAsync(long classroomId, CancellationToken ct);
    Task AddEnrollmentAsync(StudentEnrollment enrollment, CancellationToken ct);
}

// Danh mục quyền và quyền của từng vai trò.
public interface IPermissionRepository
{
    Task<bool> AnyAsync(CancellationToken ct);
    Task<bool> AnyRoleLinkAsync(CancellationToken ct);
    Task<List<Permission>> ListAsync(string? module, CancellationToken ct);
    Task<List<Permission>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct);
    Task AddRangeAsync(IEnumerable<Permission> permissions, CancellationToken ct);
    Task<List<string>> ListCodesForUserAsync(long userId, CancellationToken ct);
    Task<List<Permission>> ListForRoleAsync(long roleId, CancellationToken ct);
    Task ReplaceRoleAsync(long roleId, IReadOnlyCollection<long> permissionIds, long? actorId, DateTime now, CancellationToken ct);
    Task AddRoleLinksAsync(IEnumerable<RolePermission> links, CancellationToken ct);
}

// Giao đề cho lớp hoặc học sinh. Không có dòng giao thì đề vẫn theo danh sách thí sinh cũ.
public interface IExamAssignmentRepository
{
    Task AddAsync(ExamAssignment assignment, CancellationToken ct);
    Task<ExamAssignment?> FindActiveAsync(long examId, long? classroomId, long? studentId, CancellationToken ct);
    Task<List<ExamAssignment>> ListByClassroomAsync(long classroomId, CancellationToken ct);
    Task<bool> AllowsAsync(long examId, long userId, CancellationToken ct);
}
