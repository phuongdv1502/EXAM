using System.Reflection;
using AIQuizHSG.Application.Accounts;
using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Exams;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Security;

namespace AIQuizHSG.Tests;

// Đề đã phát hành giữ nội dung cũ. Nộp bài chỉ đổi trạng thái, chưa ghi điểm.
public class ExamFlowTests
{
    [Fact]
    public async Task Publish_FreezesQuestion_AndSubmitLeavesScoreEmpty()
    {
        var clock = new MutableClock(new DateTime(2026, 10, 28, 8, 0, 0, DateTimeKind.Utc));
        var store = new FlowStore();
        var student = store.AddStudent("hs01");
        var question = store.AddApprovedQuestion("Nội dung A");
        var exams = new ExamService(store, clock, new SaveExamRequestValidator());
        var attempts = new AttemptService(store, clock, new SaveAnswerRequestValidator());
        var teacher = new Actor(1, "127.0.0.1");
        var pupil = new Actor(student.Id, "127.0.0.1");

        var created = await exams.CreateAsync(ExamRequest(store.Subject.Id, question.Id), teacher, CancellationToken.None);
        Assert.Equal(OperationStatus.Ok, created.Status);

        question.Content = "Nội dung B";
        var published = await exams.PublishAsync(created.Value!.Id, teacher, CancellationToken.None);
        Assert.Equal("Nội dung B", published.Value!.Questions[0].Content);

        question.Content = "Nội dung C";
        var reread = await exams.GetAsync(published.Value.Id, CancellationToken.None);
        Assert.Equal("Nội dung B", reread.Value!.Questions[0].Content);
        Assert.Contains(reread.Value.Questions[0].Options, option => option.IsCorrect);

        var started = await attempts.StartAsync(published.Value.Id, pupil, CancellationToken.None);
        var paper = started.Value!;
        Assert.DoesNotContain(typeof(PaperOption).GetProperties(), property => property.Name == "IsCorrect");
        var optionId = paper.Questions[0].Options[0].Id;
        await attempts.SaveAnswerAsync(
            paper.Id,
            paper.Questions[0].ExamQuestionId,
            new SaveAnswerRequest { OptionIds = [optionId] },
            pupil,
            CancellationToken.None);

        var reloaded = await attempts.GetAsync(paper.Id, pupil, CancellationToken.None);
        Assert.Equal(optionId, Assert.Single(reloaded.Value!.Questions[0].SelectedOptionIds));

        var first = await attempts.SubmitAsync(paper.Id, pupil, CancellationToken.None);
        var second = await attempts.SubmitAsync(paper.Id, pupil, CancellationToken.None);
        Assert.Equal(AttemptStatus.Submitted, second.Value!.Status);
        Assert.Equal(first.Value!.SubmittedAtUtc, second.Value.SubmittedAtUtc);
        Assert.Null(store.Attempts.Single().Score);
        Assert.Equal(1, store.Audits.Count(action => action == AuditCodes.SubmitAttempt));

        var again = await attempts.StartAsync(published.Value.Id, pupil, CancellationToken.None);
        Assert.Equal(Messages.ExamAttemptLimit, again.Message);
    }

    [Fact]
    public async Task PastDue_SubmitsWithoutScore()
    {
        var clock = new MutableClock(new DateTime(2026, 10, 28, 8, 0, 0, DateTimeKind.Utc));
        var store = new FlowStore();
        var student = store.AddStudent("hs01");
        var question = store.AddApprovedQuestion("2 + 2");
        var exams = new ExamService(store, clock, new SaveExamRequestValidator());
        var attempts = new AttemptService(store, clock, new SaveAnswerRequestValidator());
        var teacher = new Actor(1, null);
        var pupil = new Actor(student.Id, null);

        var created = await exams.CreateAsync(ExamRequest(store.Subject.Id, question.Id), teacher, CancellationToken.None);
        var published = await exams.PublishAsync(created.Value!.Id, teacher, CancellationToken.None);
        var started = await attempts.StartAsync(published.Value!.Id, pupil, CancellationToken.None);
        var due = started.Value!.DueAtUtc;

        clock.UtcNow = due.AddMinutes(1);
        var paper = await attempts.GetAsync(started.Value.Id, pupil, CancellationToken.None);
        Assert.Equal(AttemptStatus.Submitted, paper.Value!.Status);
        Assert.Equal(due, store.Attempts.Single().SubmittedAtUtc);
        Assert.Null(store.Attempts.Single().Score);

        var late = await attempts.SaveAnswerAsync(
            started.Value.Id,
            paper.Value.Questions[0].ExamQuestionId,
            new SaveAnswerRequest { OptionIds = [paper.Value.Questions[0].Options[0].Id] },
            pupil,
            CancellationToken.None);
        Assert.Equal(Messages.AttemptFinished, late.Message);
    }

    [Fact]
    public async Task Start_RejectsStudentOutsideParticipantList()
    {
        var clock = new MutableClock(new DateTime(2026, 10, 28, 8, 0, 0, DateTimeKind.Utc));
        var store = new FlowStore();
        var allowed = store.AddStudent("hs01");
        var blocked = store.AddStudent("hs02");
        var question = store.AddApprovedQuestion("Câu 1");
        var exams = new ExamService(store, clock, new SaveExamRequestValidator());
        var attempts = new AttemptService(store, clock, new SaveAnswerRequestValidator());

        var request = ExamRequest(store.Subject.Id, question.Id);
        request.ParticipantUserIds = [allowed.Id];
        var created = await exams.CreateAsync(request, new Actor(1, null), CancellationToken.None);
        var published = await exams.PublishAsync(created.Value!.Id, new Actor(1, null), CancellationToken.None);

        var started = await attempts.StartAsync(published.Value!.Id, new Actor(blocked.Id, null), CancellationToken.None);
        Assert.Equal(Messages.ExamNotParticipant, started.Message);
    }

    private static SaveExamRequest ExamRequest(long subjectId, long questionId) => new()
    {
        Title = "Đề giữa kỳ",
        SubjectId = subjectId,
        DurationMinutes = 30,
        MaxAttempts = 1,
        Questions = [new ExamQuestionInput { QuestionId = questionId, Points = 1 }]
    };

    private sealed class MutableClock(DateTime now) : IClock
    {
        public DateTime UtcNow { get; set; } = now;
    }

    private sealed class FlowStore : IUnitOfWork
    {
        public Subject Subject { get; } = new() { Id = 1, Code = "TIN", Name = "Tin học", IsActive = true };
        public List<User> UsersData { get; } = [];
        public List<Question> QuestionsData { get; } = [];
        public List<Exam> ExamsData { get; } = [];
        public List<ExamAttempt> Attempts { get; } = [];
        public List<string> Audits { get; } = [];
        public long NextId { get; set; } = 2;

        public FlowStore()
        {
            Users = new UserRepo(this);
            Questions = new QuestionRepo(this);
            Exams = new ExamRepo(this);
            AttemptsRepo = new AttemptRepo(this);
        }

        public IUserRepository Users { get; }
        public IRoleRepository Roles => throw new NotSupportedException();
        public ISubjectRepository Subjects => new SubjectRepo(this);
        public ITopicRepository Topics => throw new NotSupportedException();
        public IQuestionRepository Questions { get; }
        public IDifficultyLevelRepository DifficultyLevels => throw new NotSupportedException();
        public IExamProfileRepository ExamProfiles => throw new NotSupportedException();
        public ISchoolRepository Schools => throw new NotSupportedException();
        public IGradeLevelRepository GradeLevels => throw new NotSupportedException();
        public IClassroomRepository Classrooms => throw new NotSupportedException();
        public IStudentRecordRepository StudentRecords => throw new NotSupportedException();
        public IPermissionRepository Permissions => throw new NotSupportedException();
        public IExamAssignmentRepository ExamAssignments { get; } = new OpenAssignments();
        public IOmrRepository Omr => throw new NotSupportedException();
        public IReportRepository Reports => throw new NotSupportedException();
        public IExamRepository Exams { get; }
        public IAttemptRepository AttemptsRepo { get; }
        IAttemptRepository IUnitOfWork.Attempts => AttemptsRepo;
        public IAuditLogRepository AuditLogs => new AuditRepo(this);

        public User AddStudent(string username)
        {
            var user = new User
            {
                Id = NextId++,
                Username = username,
                FullName = username,
                Status = UserStatus.Active,
                UserRoles = [new UserRole { Role = new Role { Code = RoleCodes.Student, Name = "Học sinh" } }]
            };
            UsersData.Add(user);
            return user;
        }

        public Question AddApprovedQuestion(string content)
        {
            var topic = new Topic { Id = NextId++, SubjectId = Subject.Id, Subject = Subject, Name = "Số học", IsActive = true };
            var question = new Question
            {
                Id = NextId++,
                TopicId = topic.Id,
                Topic = topic,
                Content = content,
                Status = QuestionStatus.Approved,
                Type = QuestionType.SingleChoice,
                Level = QuestionLevel.Easy,
                Options =
                [
                    new QuestionOption { Id = NextId++, Content = "Đúng", IsCorrect = true, SortOrder = 1 },
                    new QuestionOption { Id = NextId++, Content = "Sai", IsCorrect = false, SortOrder = 2 }
                ]
            };
            QuestionsData.Add(question);
            return question;
        }

        public Task<int> SaveChangesAsync(CancellationToken ct)
        {
            foreach (var exam in ExamsData)
            {
                if (exam.Id == 0)
                    exam.Id = NextId++;
                foreach (var question in exam.Questions)
                {
                    if (question.Id == 0)
                        question.Id = NextId++;
                    question.ExamId = exam.Id;
                    foreach (var option in question.Options)
                    {
                        if (option.Id == 0)
                            option.Id = NextId++;
                        option.ExamQuestionId = question.Id;
                    }
                }

                foreach (var participant in exam.Participants)
                {
                    if (participant.Id == 0)
                        participant.Id = NextId++;
                    participant.ExamId = exam.Id;
                }
            }

            foreach (var attempt in Attempts)
            {
                if (attempt.Id == 0)
                    attempt.Id = NextId++;
                foreach (var answer in attempt.Answers)
                {
                    if (answer.Id == 0)
                        answer.Id = NextId++;
                    answer.AttemptId = attempt.Id;
                    foreach (var selected in answer.SelectedOptions)
                        selected.AttemptAnswerId = answer.Id;
                }
            }

            return Task.FromResult(1);
        }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct) => action(ct);

        public Task<bool> CanConnectAsync(CancellationToken ct) => Task.FromResult(true);

        private sealed class SubjectRepo(FlowStore owner) : ISubjectRepository
        {
            public Task<Subject?> FindAsync(long id, CancellationToken ct) =>
                Task.FromResult(owner.Subject.Id == id ? owner.Subject : null);

            public Task<bool> AnyAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<List<Subject>> ListAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<Subject?> FindByCodeAsync(string code, CancellationToken ct) => throw new NotSupportedException();
            public Task AddAsync(Subject subject, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class UserRepo(FlowStore owner) : IUserRepository
        {
            public Task<List<User>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct) =>
                Task.FromResult(owner.UsersData.Where(user => ids.Contains(user.Id)).ToList());

            public Task<bool> AnyAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<User?> FindByUsernameAsync(string username, CancellationToken ct) => throw new NotSupportedException();
            public Task<User?> FindByIdAsync(long id, CancellationToken ct) => throw new NotSupportedException();
            public Task<User?> FindForUpdateAsync(long id, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<User>> ListByRoleAsync(string roleCode, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<User>> ListAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task AddAsync(User user, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class QuestionRepo(FlowStore owner) : IQuestionRepository
        {
            public Task<List<Question>> ListWithOptionsAsync(IReadOnlyCollection<long> ids, CancellationToken ct) =>
                Task.FromResult(owner.QuestionsData.Where(question => ids.Contains(question.Id)).ToList());

            public Task<List<Question>> ListEligibleAsync(long subjectId, IReadOnlyCollection<long> topicIds, QuestionType? type, CancellationToken ct) =>
                throw new NotSupportedException();

            public Task<Question?> FindAsync(long id, CancellationToken ct) => throw new NotSupportedException();
            public Task<(int Total, List<Question> Items)> SearchAsync(QuestionFilter filter, CancellationToken ct) => throw new NotSupportedException();
            public Task AddAsync(Question question, CancellationToken ct) => throw new NotSupportedException();
            public void ReplaceOptions(Question question, IReadOnlyList<QuestionOption> options) => throw new NotSupportedException();
        }

        private sealed class ExamRepo(FlowStore owner) : IExamRepository
        {
            public Task<Exam?> FindAsync(long id, CancellationToken ct) =>
                Task.FromResult(owner.ExamsData.FirstOrDefault(exam => exam.Id == id));

            public Task AddAsync(Exam exam, CancellationToken ct)
            {
                owner.ExamsData.Add(exam);
                return Task.CompletedTask;
            }

            public void ReplaceQuestions(Exam exam, IReadOnlyList<ExamQuestion> questions)
            {
                exam.Questions.Clear();
                foreach (var question in questions)
                    exam.Questions.Add(question);
            }

            public void ReplaceParticipants(Exam exam, IReadOnlyList<long> userIds)
            {
                exam.Participants.Clear();
                foreach (var userId in userIds)
                    exam.Participants.Add(new ExamParticipant { UserId = userId });
            }

            public Task<List<ExamSummary>> ListAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<List<ExamSummary>> ListOpenForStudentAsync(long userId, CancellationToken ct) => throw new NotSupportedException();
            public Task<Exam?> FindByGenerationKeyAsync(string key, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class AttemptRepo(FlowStore owner) : IAttemptRepository
        {
            public Task<int> CountAsync(long examId, long userId, CancellationToken ct) =>
                Task.FromResult(owner.Attempts.Count(attempt => attempt.ExamId == examId && attempt.UserId == userId));

            public Task<ExamAttempt?> FindInProgressAsync(long examId, long userId, CancellationToken ct) =>
                Task.FromResult(owner.Attempts.FirstOrDefault(attempt =>
                    attempt.ExamId == examId && attempt.UserId == userId && attempt.Status == AttemptStatus.InProgress));

            public Task<ExamAttempt?> FindAsync(long id, CancellationToken ct) =>
                Task.FromResult(owner.Attempts.FirstOrDefault(attempt => attempt.Id == id));

            public Task<ExamAttempt?> FindLatestAsync(long examId, long userId, CancellationToken ct) =>
                Task.FromResult(owner.Attempts
                    .Where(attempt => attempt.ExamId == examId && attempt.UserId == userId)
                    .OrderByDescending(attempt => attempt.AttemptNumber)
                    .FirstOrDefault());

            public Task AddAsync(ExamAttempt attempt, CancellationToken ct)
            {
                owner.Attempts.Add(attempt);
                return Task.CompletedTask;
            }

            public void ReplaceSelections(AttemptAnswer answer, IReadOnlyList<long> optionIds)
            {
                answer.SelectedOptions.Clear();
                foreach (var optionId in optionIds)
                {
                    answer.SelectedOptions.Add(new AttemptAnswerOption
                    {
                        ExamQuestionId = answer.ExamQuestionId,
                        ExamOptionId = optionId
                    });
                }
            }

            public Task<List<ExamAttempt>> ListByUserAsync(long userId, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<ExamAttempt>> ListByUserIdsAsync(IReadOnlyCollection<long> userIds, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class OpenAssignments : IExamAssignmentRepository
        {
            public Task<bool> AllowsAsync(long examId, long userId, CancellationToken ct) => Task.FromResult(true);
            public Task AddAsync(ExamAssignment assignment, CancellationToken ct) => throw new NotSupportedException();
            public Task<ExamAssignment?> FindActiveAsync(long examId, long? classroomId, long? studentId, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<ExamAssignment>> ListByClassroomAsync(long classroomId, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<ExamAssignment>> ListByExamAsync(long examId, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class AuditRepo(FlowStore owner) : IAuditLogRepository
        {
            public void Add(long? userId, string action, string entityName, long? entityId, string? details, string? ipAddress) =>
                owner.Audits.Add(action);
        }
    }
}

public class AccountServiceTests
{
    [Fact]
    public async Task Create_RejectsDuplicateUsername_AndLockChangesStatus()
    {
        var store = new AccountStore();
        var service = new AccountService(
            store,
            new PasswordHasherService(),
            new SystemClock(),
            new CreateStudentRequestValidator(),
            new UpdateStudentRequestValidator());
        var actor = new Actor(1, null);
        var request = new CreateStudentRequest
        {
            Username = "hs01",
            Password = "12345678",
            FullName = "Học sinh 1"
        };

        var created = await service.CreateAsync(request, actor, CancellationToken.None);
        var duplicate = await service.CreateAsync(request, actor, CancellationToken.None);
        var locked = await service.LockAsync(created.Value!.Id, actor, CancellationToken.None);

        Assert.Equal(OperationStatus.Ok, created.Status);
        Assert.NotEqual("12345678", store.People.Single().PasswordHash);
        Assert.Equal(Messages.UsernameTaken, duplicate.Message);
        Assert.Equal(UserStatus.Locked, locked.Value!.Status);
    }

    private sealed class AccountStore : IUnitOfWork
    {
        public List<User> People { get; } = [];
        public Role StudentRole { get; } = new() { Id = 1, Code = RoleCodes.Student, Name = "Học sinh" };
        public long NextId { get; set; } = 10;

        public AccountStore()
        {
            Users = new UserRepo(this);
            Roles = new RoleRepo(this);
        }

        public IUserRepository Users { get; }
        public IRoleRepository Roles { get; }
        public ISubjectRepository Subjects => throw new NotSupportedException();
        public ITopicRepository Topics => throw new NotSupportedException();
        public IQuestionRepository Questions => throw new NotSupportedException();
        public IDifficultyLevelRepository DifficultyLevels => throw new NotSupportedException();
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
        public IAuditLogRepository AuditLogs { get; } = new AuditRepo();

        public Task<int> SaveChangesAsync(CancellationToken ct)
        {
            foreach (var user in People.Where(user => user.Id == 0))
                user.Id = NextId++;
            return Task.FromResult(1);
        }

        public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct) => action(ct);
        public Task<bool> CanConnectAsync(CancellationToken ct) => Task.FromResult(true);

        private sealed class UserRepo(AccountStore owner) : IUserRepository
        {
            public Task<User?> FindByUsernameAsync(string username, CancellationToken ct) =>
                Task.FromResult(owner.People.FirstOrDefault(user => user.Username == username));

            public Task<User?> FindForUpdateAsync(long id, CancellationToken ct) =>
                Task.FromResult(owner.People.FirstOrDefault(user => user.Id == id));

            public Task AddAsync(User user, CancellationToken ct)
            {
                owner.People.Add(user);
                return Task.CompletedTask;
            }

            public Task<bool> AnyAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<User?> FindByIdAsync(long id, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<User>> ListByIdsAsync(IReadOnlyCollection<long> ids, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<User>> ListByRoleAsync(string roleCode, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<User>> ListAsync(CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class RoleRepo(AccountStore owner) : IRoleRepository
        {
            public Task<Role?> FindByCodeAsync(string code, CancellationToken ct) =>
                Task.FromResult(owner.StudentRole.Code == code ? owner.StudentRole : null);

            public Task<bool> AnyAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task AddRangeAsync(IEnumerable<Role> roles, CancellationToken ct) => throw new NotSupportedException();
            public Task<List<Role>> ListAsync(CancellationToken ct) => throw new NotSupportedException();
            public Task<Role?> FindAsync(long id, CancellationToken ct) => throw new NotSupportedException();
        }

        private sealed class AuditRepo : IAuditLogRepository
        {
            public void Add(long? userId, string action, string entityName, long? entityId, string? details, string? ipAddress)
            {
            }
        }
    }
}
