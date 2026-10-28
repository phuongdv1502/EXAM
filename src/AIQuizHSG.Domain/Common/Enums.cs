namespace AIQuizHSG.Domain.Common;

// Giá trị lưu trong MySQL bằng tên tiếng Anh, không lưu số thứ tự.
public enum UserStatus
{
    Active,
    Locked
}

public enum QuestionLevel
{
    Easy,
    Medium,
    Hard
}

public enum QuestionType
{
    SingleChoice,
    MultipleChoice
}

public enum QuestionStatus
{
    Draft,
    PendingReview,
    Approved,
    Rejected
}

public enum QuestionSource
{
    Manual,
    Ai
}

public enum ExamStatus
{
    Draft,
    Published,
    Closed
}

public enum AttemptStatus
{
    InProgress,
    Submitted,
    Graded
}

public enum AiJobStatus
{
    Pending,
    Succeeded,
    Failed
}

public enum ScalingMode
{
    Proportional,
    FixedCounts
}

public enum ProfileStatus
{
    Draft,
    Active,
    Archived
}

public enum SchoolYearStatus
{
    Planned,
    Open,
    Closed
}

public enum ClassroomStatus
{
    Active,
    Archived
}

public enum StudentProfileStatus
{
    Active,
    Inactive
}

public enum Gender
{
    Male,
    Female,
    Other
}

public enum EnrollmentStatus
{
    Active,
    Transferred,
    Left
}

public enum SchoolRole
{
    SchoolAdmin,
    Teacher,
    Staff
}

public enum MembershipStatus
{
    Active,
    Ended
}

public enum ClassRole
{
    SubjectTeacher
}

public enum AssignmentStatus
{
    Active,
    Cancelled
}

// Quyền gắn thẳng vào một người. Từ chối thắng quyền được cấp qua vai trò.
public enum PermissionEffect
{
    Allow,
    Deny
}

public enum OmrBatchStatus
{
    Draft,
    Uploading,
    Processing,
    Reviewing,
    Approved,
    Published
}

public enum OmrSheetStatus
{
    Uploaded,
    Processing,
    NeedsReview,
    Recognized,
    Confirmed,
    Graded,
    Failed
}

public enum OmrReviewStatus
{
    Pending,
    Confirmed
}

public enum OmrSourceType
{
    Camera,
    Scanner,
    Mixed
}

public enum OmrMarkStatus
{
    Blank,
    Marked,
    Ambiguous
}
