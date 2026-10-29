using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Domain.Questions;
using ClosedXML.Excel;

namespace AIQuizHSG.Tests;

// Một đáp án đúng đúng một phương án. Nhiều đáp án vẫn phải có phương án sai.
public class QuestionRulesTests
{
    [Fact]
    public void SingleChoice_RequiresExactlyOneCorrectOption()
    {
        var valid = QuestionRules.ValidateOptions(QuestionType.SingleChoice, [("A", false), ("B", true)]);
        var twoCorrect = QuestionRules.ValidateOptions(QuestionType.SingleChoice, [("A", true), ("B", true)]);

        Assert.Null(valid);
        Assert.NotNull(twoCorrect);
    }

    [Fact]
    public void MultipleChoice_RequiresAWrongOption()
    {
        var valid = QuestionRules.ValidateOptions(QuestionType.MultipleChoice, [("A", true), ("B", true), ("C", false)]);
        var allCorrect = QuestionRules.ValidateOptions(QuestionType.MultipleChoice, [("A", true), ("B", true)]);

        Assert.Null(valid);
        Assert.NotNull(allCorrect);
    }
}

// Dòng Excel có mức độ lạ thì báo lỗi đúng số dòng, không nhập câu đó.
public class QuestionExcelTests
{
    [Fact]
    public void Read_InvalidLevel_ReportsTheRow()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("CauHoi");
        string[] headers = ["MaMon", "ChuyenDe", "NoiDung", "MucDo", "Loai", "PhuongAn1", "Dung1", "PhuongAn2", "Dung2"];
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];
        sheet.Cell(2, 1).Value = "TIN";
        sheet.Cell(2, 2).Value = "Thuật toán";
        sheet.Cell(2, 3).Value = "Nội dung";
        sheet.Cell(2, 4).Value = "Rất khó";
        sheet.Cell(2, 5).Value = "Một đáp án";
        sheet.Cell(2, 6).Value = "A";
        sheet.Cell(2, 7).Value = "Đúng";
        sheet.Cell(2, 8).Value = "B";
        sheet.Cell(2, 9).Value = "Sai";

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;

        var result = QuestionExcel.Read(stream);

        Assert.Empty(result.Rows);
        Assert.Contains(result.Errors, error => error.Row == 2 && error.Message.Contains("MucDo"));
    }
}

// Luồng câu hỏi: từ chối đáp án sai, chỉ duyệt sau khi gửi, sửa câu đã duyệt thì chờ duyệt lại.
public class QuestionServiceTests
{
    private readonly Actor _actor = new(5, "127.0.0.1");

    // Câu một đáp án mà đánh hai phương án đúng thì không được tạo.
    [Fact]
    public async Task Create_RejectsQuestionWithoutASingleCorrectAnswer()
    {
        var service = CreateService(out _);
        var request = Request(correctCount: 1);
        request.Type = QuestionType.SingleChoice;
        request.Options[1].IsCorrect = true;
        var result = await service.CreateAsync(request, _actor, CancellationToken.None);

        Assert.Equal(OperationStatus.Invalid, result.Status);
    }

    // Duyệt khi còn nháp thì lỗi. Sau khi duyệt, sửa nội dung đưa câu về chờ duyệt.
    [Fact]
    public async Task Approve_OnlyWorksAfterSubmit_AndEditReturnsToPendingReview()
    {
        var service = CreateService(out var unitOfWork);
        var created = await service.CreateAsync(Request(correctCount: 1), _actor, CancellationToken.None);
        var id = created.Value!.Id;

        var early = await service.ApproveAsync(id, _actor, CancellationToken.None);
        Assert.Equal(OperationStatus.Invalid, early.Status);

        var submitted = await service.SubmitAsync(id, _actor, CancellationToken.None);
        Assert.Equal(QuestionStatus.PendingReview, submitted.Value!.Status);

        var approved = await service.ApproveAsync(id, _actor, CancellationToken.None);
        Assert.Equal(QuestionStatus.Approved, approved.Value!.Status);

        var edited = await service.UpdateAsync(id, Request(correctCount: 1, content: "Nội dung đã sửa"), _actor, CancellationToken.None);
        Assert.Equal(QuestionStatus.PendingReview, edited.Value!.Status);
        Assert.Equal(QuestionStatus.PendingReview, unitOfWork.QuestionsRepo.Items.Single().Status);
    }

    // Chuyên đề không có thì bỏ dòng. Dòng khớp môn và chuyên đề vào thẳng chờ duyệt.
    [Fact]
    public async Task Import_SkipsUnknownTopic_AndImportsKnownTopicAsPendingReview()
    {
        var service = CreateService(out var unitOfWork);
        unitOfWork.TopicsRepo.Items.Add(new Topic
        {
            Id = 2,
            SubjectId = 1,
            Subject = unitOfWork.SubjectsRepo.Items[0],
            Name = "Thuật toán",
            IsActive = true
        });

        await using var stream = new MemoryStream(QuestionExcel.CreateTemplate());
        var result = await service.ImportAsync(stream, _actor, CancellationToken.None);

        Assert.Equal(OperationStatus.Ok, result.Status);
        Assert.Equal(1, result.Value!.Imported);
        Assert.Equal(QuestionStatus.PendingReview, Assert.Single(unitOfWork.QuestionsRepo.Items).Status);
    }

    private static SaveQuestionRequest Request(int correctCount, string content = "2 + 2 bằng mấy?") => new()
    {
        TopicId = 1,
        Content = content,
        Level = QuestionLevel.Easy,
        Type = correctCount == 1 ? QuestionType.SingleChoice : QuestionType.MultipleChoice,
        Options =
        [
            new OptionInput { Content = "4", IsCorrect = true },
            new OptionInput { Content = "5", IsCorrect = correctCount > 1 },
            new OptionInput { Content = "6", IsCorrect = false }
        ]
    };

    private QuestionService CreateService(out MemoryUnitOfWork unitOfWork)
    {
        unitOfWork = new MemoryUnitOfWork();
        var subject = new Subject { Id = 1, Code = "TIN", Name = "Tin học", IsActive = true };
        unitOfWork.SubjectsRepo.Items.Add(subject);
        unitOfWork.TopicsRepo.Items.Add(new Topic
        {
            Id = 1,
            SubjectId = 1,
            Subject = subject,
            Name = "Số học",
            IsActive = true
        });
        unitOfWork.NextId = 10;
        return new QuestionService(unitOfWork, new SaveQuestionRequestValidator(), new RejectQuestionRequestValidator());
    }

    // Giả lập Unit of Work trong bộ nhớ. SaveChanges không ghi database, transaction chỉ chạy tiếp action.
    private sealed class MemoryUnitOfWork : IUnitOfWork
    {
        public MemorySubjects SubjectsRepo { get; } = new();
        public MemoryTopics TopicsRepo { get; }
        public MemoryQuestions QuestionsRepo { get; }
        public long NextId { get; set; } = 1;

        public MemoryUnitOfWork()
        {
            TopicsRepo = new MemoryTopics(this);
            QuestionsRepo = new MemoryQuestions(this);
        }

        public IUserRepository Users => throw new NotSupportedException();
        public IRoleRepository Roles => throw new NotSupportedException();
        public ISubjectRepository Subjects => SubjectsRepo;
        public ITopicRepository Topics => TopicsRepo;
        public IQuestionRepository Questions => QuestionsRepo;
        public IDifficultyLevelRepository DifficultyLevels { get; } = new MemoryLevels();
        public IExamProfileRepository ExamProfiles => throw new NotSupportedException();
        public ISchoolRepository Schools => throw new NotSupportedException();
        public IGradeLevelRepository GradeLevels => throw new NotSupportedException();
        public IClassroomRepository Classrooms => throw new NotSupportedException();
        public IStudentRecordRepository StudentRecords => throw new NotSupportedException();
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IExamAssignmentRepository ExamAssignments => throw new NotSupportedException();
        public IOmrRepository Omr => throw new NotSupportedException();
        public IReportRepository Reports => throw new NotSupportedException();
        public IExamRepository Exams => throw new NotSupportedException();
        public IAttemptRepository Attempts => throw new NotSupportedException();
        public IAuditLogRepository AuditLogs { get; } = new MemoryAudit();

        public Task<int> SaveChangesAsync(CancellationToken ct) => Task.FromResult(0);

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct) => action(ct);

        public Task<bool> CanConnectAsync(CancellationToken ct) => Task.FromResult(true);
    }

    private sealed class MemorySubjects : ISubjectRepository
    {
        public List<Subject> Items { get; } = [];

        public Task<bool> AnyAsync(CancellationToken ct) => Task.FromResult(Items.Count > 0);
        public Task<List<Subject>> ListAsync(CancellationToken ct) => Task.FromResult(Items.ToList());
        public Task<Subject?> FindAsync(long id, CancellationToken ct) =>
            Task.FromResult(Items.FirstOrDefault(subject => subject.Id == id));
        public Task<Subject?> FindByCodeAsync(string code, CancellationToken ct) =>
            Task.FromResult(Items.FirstOrDefault(subject => subject.Code.Equals(code, StringComparison.OrdinalIgnoreCase)));
        public Task AddAsync(Subject subject, CancellationToken ct)
        {
            Items.Add(subject);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryTopics(QuestionServiceTests.MemoryUnitOfWork owner) : ITopicRepository
    {
        public List<Topic> Items { get; } = [];

        public Task<List<Topic>> ListAsync(long? subjectId, CancellationToken ct) =>
            Task.FromResult(Items.Where(topic => subjectId is null || topic.SubjectId == subjectId).ToList());

        public Task<Topic?> FindAsync(long id, CancellationToken ct) =>
            Task.FromResult(Items.FirstOrDefault(topic => topic.Id == id));

        public Task AddAsync(Topic topic, CancellationToken ct)
        {
            topic.Id = owner.NextId++;
            topic.Subject = owner.SubjectsRepo.Items.Single(subject => subject.Id == topic.SubjectId);
            Items.Add(topic);
            return Task.CompletedTask;
        }
    }

    private sealed class MemoryQuestions(QuestionServiceTests.MemoryUnitOfWork owner) : IQuestionRepository
    {
        public List<Question> Items { get; } = [];

        public Task<Question?> FindAsync(long id, CancellationToken ct) =>
            Task.FromResult(Items.FirstOrDefault(question => question.Id == id));

        public Task<(int Total, List<Question> Items)> SearchAsync(QuestionFilter filter, CancellationToken ct)
        {
            var items = Items.ToList();
            return Task.FromResult((items.Count, items));
        }

        public Task<List<Question>> ListWithOptionsAsync(IReadOnlyCollection<long> ids, CancellationToken ct) =>
            Task.FromResult(Items.Where(question => ids.Contains(question.Id)).ToList());

        public Task<List<Question>> ListEligibleAsync(long subjectId, IReadOnlyCollection<long> topicIds, QuestionType? type, CancellationToken ct) =>
            throw new NotSupportedException();

        public Task AddAsync(Question question, CancellationToken ct)
        {
            question.Id = owner.NextId++;
            var order = 1;
            foreach (var option in question.Options)
            {
                option.Id = owner.NextId++;
                option.SortOrder = order++;
            }

            question.Topic = owner.TopicsRepo.Items.Single(topic => topic.Id == question.TopicId);
            Items.Add(question);
            return Task.CompletedTask;
        }

        public void ReplaceOptions(Question question, IReadOnlyList<QuestionOption> options)
        {
            question.Options.Clear();
            var order = 1;
            foreach (var option in options)
            {
                option.Id = owner.NextId++;
                option.SortOrder = order++;
                question.Options.Add(option);
            }
        }
    }

    private sealed class MemoryAudit : IAuditLogRepository
    {
        public void Add(long? userId, string action, string entityName, long? entityId, string? details, string? ipAddress)
        {
        }
    }

    private sealed class MemoryLevels : IDifficultyLevelRepository
    {
        private static readonly DifficultyLevel Applied = new()
        {
            Id = 3,
            Code = "3",
            Name = "Vận dụng",
            SortOrder = 3,
            IsActive = true
        };

        public Task<bool> AnyAsync(CancellationToken ct) => Task.FromResult(true);
        public Task<List<DifficultyLevel>> ListActiveAsync(CancellationToken ct) => Task.FromResult(new List<DifficultyLevel> { Applied });
        public Task<DifficultyLevel?> FindAsync(byte id, CancellationToken ct) =>
            Task.FromResult(id == Applied.Id ? Applied : null);
        public Task<DifficultyLevel?> FindByCodeAsync(string code, CancellationToken ct) =>
            Task.FromResult(code == Applied.Code ? Applied : null);
        public Task AddRangeAsync(IEnumerable<DifficultyLevel> levels, CancellationToken ct) => Task.CompletedTask;
    }
}
