namespace AIQuizHSG.Domain.Common;

// Tên thao tác và tên đối tượng ghi vào nhật ký. Service không viết các chuỗi này trực tiếp.
public static class AuditCodes
{
    public const string User = "User";
    public const string Subject = "Subject";
    public const string Topic = "Topic";
    public const string Question = "Question";

    // Không phải mật khẩu thật. Chỉ băm khi tên đăng nhập không tồn tại để thời gian phản hồi không lộ tài khoản.
    public const string TimingProbePassword = "timing-equalization";

    public const string Login = "Login";
    public const string LoginFailed = "LoginFailed";
    public const string LoginRejected = "LoginRejected";

    public const string CreateSubject = "CreateSubject";
    public const string UpdateSubject = "UpdateSubject";
    public const string CreateTopic = "CreateTopic";
    public const string UpdateTopic = "UpdateTopic";

    public const string CreateQuestion = "CreateQuestion";
    public const string UpdateQuestion = "UpdateQuestion";
    public const string SubmitQuestion = "SubmitQuestion";
    public const string ApproveQuestion = "ApproveQuestion";
    public const string RejectQuestion = "RejectQuestion";
    public const string ImportQuestions = "ImportQuestions";

    public const string Exam = "Exam";
    public const string Attempt = "Attempt";

    public const string CreateStudent = "CreateStudent";
    public const string UpdateStudent = "UpdateStudent";
    public const string LockStudent = "LockStudent";
    public const string UnlockStudent = "UnlockStudent";

    public const string CreateExam = "CreateExam";
    public const string UpdateExam = "UpdateExam";
    public const string PublishExam = "PublishExam";
    public const string CloseExam = "CloseExam";
    public const string StartAttempt = "StartAttempt";
    public const string SubmitAttempt = "SubmitAttempt";

    public const string DifficultyProfile = "ExamDifficultyProfile";
    public const string CreateProfile = "CreateProfile";
    public const string UpdateProfile = "UpdateProfile";
    public const string GenerateExam = "GenerateExam";

    public const string School = "School";
    public const string SchoolYear = "SchoolYear";
    public const string GradeLevel = "GradeLevel";
    public const string Classroom = "Classroom";
    public const string StudentProfile = "StudentProfile";
    public const string Enrollment = "StudentEnrollment";
    public const string Permission = "Permission";
    public const string ExamAssignment = "ExamAssignment";

    public const string CreateSchool = "CreateSchool";
    public const string UpdateSchool = "UpdateSchool";
    public const string CreateSchoolYear = "CreateSchoolYear";
    public const string UpdateSchoolYear = "UpdateSchoolYear";
    public const string CreateGradeLevel = "CreateGradeLevel";
    public const string CreateClassroom = "CreateClassroom";
    public const string UpdateClassroom = "UpdateClassroom";
    public const string AssignTeacher = "AssignTeacher";
    public const string CreateStudentProfile = "CreateStudentProfile";
    public const string UpdateStudentProfile = "UpdateStudentProfile";
    public const string EnrollStudent = "EnrollStudent";
    public const string TransferStudent = "TransferStudent";
    public const string ImportStudents = "ImportStudents";
    public const string AddSchoolMember = "AddSchoolMember";
    public const string ReplaceRolePermissions = "ReplaceRolePermissions";
    public const string ReplaceUserRoles = "ReplaceUserRoles";
    public const string ReplaceUserPermissions = "ReplaceUserPermissions";
    public const string CreateAccount = "CreateAccount";
    public const string UpdateAccount = "UpdateAccount";
    public const string LockAccount = "LockAccount";
    public const string UnlockAccount = "UnlockAccount";
    public const string AssignExam = "AssignExam";
    public const string ReplaceTeachingSubjects = "ReplaceTeachingSubjects";
    public const string DrawExams = "DrawExams";

    public const string OmrTemplate = "OmrTemplate";
    public const string OmrBatch = "OmrImportBatch";
    public const string OmrSheet = "OmrScannedSheet";
    public const string CreateOmrTemplate = "CreateOmrTemplate";
    public const string CreateOmrBatch = "CreateOmrBatch";
    public const string UploadOmrSheets = "UploadOmrSheets";
    public const string ScanOmrSheet = "ScanOmrSheet";
    public const string ReviewOmrSheet = "ReviewOmrSheet";
    public const string GradeOmrBatch = "GradeOmrBatch";
    public const string ApproveOmrBatch = "ApproveOmrBatch";
    public const string PublishOmrBatch = "PublishOmrBatch";
}
