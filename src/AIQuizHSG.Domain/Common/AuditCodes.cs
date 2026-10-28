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
}
