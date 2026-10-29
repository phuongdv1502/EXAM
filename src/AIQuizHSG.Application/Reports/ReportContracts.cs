namespace AIQuizHSG.Application.Reports;

public sealed record CountItem(string Key, int Count);

public sealed record SubjectCount(string Name, int Count);

public sealed record DifficultyCount(string Name, int SortOrder, int Count);

public sealed record ExamReportLine(
    long Id,
    string Title,
    string SubjectName,
    string Status,
    int QuestionCount,
    int Submitted,
    int InProgress,
    decimal? AveragePercent);

public sealed record DashboardReport(
    int QuestionCount,
    int ExamCount,
    int AttemptCount,
    int StudentCount,
    int SubjectCount,
    int OmrSheetCount,
    int OmrNeedsReview,
    IReadOnlyList<CountItem> QuestionStatus,
    IReadOnlyList<DifficultyCount> QuestionDifficulty,
    IReadOnlyList<SubjectCount> QuestionsBySubject,
    IReadOnlyList<CountItem> ExamStatus,
    IReadOnlyList<CountItem> AttemptStatus,
    IReadOnlyList<CountItem> ScoreBands,
    IReadOnlyList<ExamReportLine> Exams,
    IReadOnlyList<CountItem> OmrStatus);
