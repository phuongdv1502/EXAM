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
