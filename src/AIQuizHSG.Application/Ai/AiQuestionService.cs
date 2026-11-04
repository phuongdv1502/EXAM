using System.Text.Json;
using AIQuizHSG.Application.Accounts;
using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Mappers;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Domain.Questions;

namespace AIQuizHSG.Application.Ai;

// Sinh câu bằng LLM, lưu chờ duyệt. Không đưa thẳng vào đề.
public sealed class AiQuestionService(IUnitOfWork unitOfWork, IAiChatClient chat, IClock clock) : IAiQuestionService
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public async Task<OperationResult<GenerateAiQuestionsResult>> GenerateAsync(
        GenerateAiQuestionsRequest request,
        Actor actor,
        CancellationToken ct)
    {
        if (request.QuestionCount is < 1 or > 10)
            return OperationResult<GenerateAiQuestionsResult>.Invalid(Messages.AiQuestionCountInvalid);
        if (request.Type == QuestionType.MultipleChoice)
            return OperationResult<GenerateAiQuestionsResult>.Invalid(Messages.AiMultipleChoiceUnsupported);

        var topic = await unitOfWork.Topics.FindAsync(request.TopicId, ct);
        if (topic is null || !topic.IsActive)
            return OperationResult<GenerateAiQuestionsResult>.Invalid(Messages.TopicInactive);
        if (await TeachingGuard.DenyAsync(unitOfWork, actor.UserId, topic.SubjectId, ct) is { } denied)
            return OperationResult<GenerateAiQuestionsResult>.Forbidden(denied);

        var now = clock.UtcNow;
        var job = new AiQuestionRequest
        {
            RequestedById = actor.UserId,
            TopicId = topic.Id,
            Level = request.Level,
            Type = request.Type,
            SourceMaterial = string.IsNullOrWhiteSpace(request.Hint) ? null : request.Hint.Trim(),
            QuestionCount = request.QuestionCount,
            Status = AiJobStatus.Pending,
            CreatedAtUtc = now
        };
        await unitOfWork.AiQuestions.AddAsync(job, ct);
        await unitOfWork.SaveChangesAsync(ct);

        var subjectName = topic.Subject?.Name;
        if (string.IsNullOrWhiteSpace(subjectName))
        {
            var subject = await unitOfWork.Subjects.FindAsync(topic.SubjectId, ct);
            subjectName = subject?.Name ?? string.Empty;
        }

        var chatResult = await chat.CompleteAsync(
            [
                new AiChatMessage("system", SystemPrompt()),
                new AiChatMessage("user", UserPrompt(subjectName!, topic.Name, request))
            ],
            ct);
        if (!chatResult.Ok)
        {
            await FailJobAsync(job, chatResult.Error, chatResult.Content, ct);
            return OperationResult<GenerateAiQuestionsResult>.Invalid(chatResult.Error ?? Messages.AiCallUnexpected);
        }

        if (!TryParseQuestions(chatResult.Content!, out var drafted, out var parseError))
        {
            await FailJobAsync(job, parseError, chatResult.Content, ct);
            return OperationResult<GenerateAiQuestionsResult>.Invalid(parseError ?? Messages.AiParseFailed);
        }

        var difficultyId = await ResolveDifficultyIdAsync(request.Level, ct);
        var errors = new List<string>();
        var created = new List<Question>();
        foreach (var item in drafted.Take(request.QuestionCount))
        {
            var options = NormalizeOptions(request.Type, item.Options);
            var pairs = options.Select(option => (option.Content, option.IsCorrect)).ToList();
            var ruleError = QuestionRules.ValidateOptions(request.Type, pairs);
            if (ruleError is not null || string.IsNullOrWhiteSpace(item.Content))
            {
                errors.Add(ruleError ?? Messages.QuestionContentRequired);
                continue;
            }

            var question = QuestionMapper.CreateFromAi(
                topic.Id,
                request.Type,
                request.Level,
                item.Content!,
                item.Explanation,
                options,
                difficultyId,
                actor,
                clock.UtcNow);
            await unitOfWork.Questions.AddAsync(question, ct);
            created.Add(question);
        }

        if (created.Count == 0)
        {
            await FailJobAsync(job, errors.Count > 0 ? string.Join("; ", errors.Take(5)) : Messages.AiNoValidQuestions, chatResult.Content, ct);
            return OperationResult<GenerateAiQuestionsResult>.Invalid(Messages.AiNoValidQuestions);
        }

        job.Status = AiJobStatus.Succeeded;
        job.ResultJson = chatResult.Content;
        job.ErrorMessage = errors.Count == 0 ? null : string.Join("; ", errors.Take(8));
        job.CompletedAtUtc = clock.UtcNow;
        await unitOfWork.SaveWithAuditAsync(
            actor.UserId,
            AuditCodes.GenerateAiQuestions,
            AuditCodes.AiQuestionRequest,
            () => job.Id,
            string.Format(Messages.ImportAudit, created.Count),
            actor.IpAddress,
            ct);

        return OperationResult<GenerateAiQuestionsResult>.Ok(
            new GenerateAiQuestionsResult(job.Id, created.Count, created.Select(item => item.Id).ToArray(), errors));
    }

    private async Task FailJobAsync(AiQuestionRequest job, string? error, string? raw, CancellationToken ct)
    {
        job.Status = AiJobStatus.Failed;
        job.ErrorMessage = error;
        job.ResultJson = raw;
        job.CompletedAtUtc = clock.UtcNow;
        await unitOfWork.SaveChangesAsync(ct);
    }

    private async Task<byte?> ResolveDifficultyIdAsync(QuestionLevel level, CancellationToken ct)
    {
        var code = level switch
        {
            QuestionLevel.Easy => "1",
            QuestionLevel.Hard => "5",
            _ => "3"
        };
        var row = await unitOfWork.DifficultyLevels.FindByCodeAsync(code, ct);
        return row is { IsActive: true } ? row.Id : null;
    }

    private static List<OptionInput> NormalizeOptions(QuestionType type, List<DraftOption>? raw)
    {
        var options = (raw ?? [])
            .Select(option => new OptionInput
            {
                Content = (option.Content ?? string.Empty).Trim(),
                IsCorrect = option.IsCorrect
            })
            .Where(option => !string.IsNullOrWhiteSpace(option.Content))
            .ToList();

        if (type != QuestionType.TrueFalse || options.Count == 0)
            return options;

        var correctIsDung = options.Any(option => option.IsCorrect && QuestionRules.IsDung(option.Content))
            || (!options.Any(option => option.IsCorrect && QuestionRules.IsSai(option.Content))
                && options[0].IsCorrect);
        return
        [
            new OptionInput { Content = "Đúng", IsCorrect = correctIsDung },
            new OptionInput { Content = "Sai", IsCorrect = !correctIsDung }
        ];
    }

    private static string SystemPrompt() =>
        """
        Bạn là giáo viên Tin học THCS Việt Nam. Chỉ trả về JSON hợp lệ, không markdown.
        Schema:
        {"questions":[{"content":"...","explanation":"...","options":[{"content":"...","isCorrect":true}]}]}
        Quy tắc:
        - SingleChoice: 4 phương án, đúng đúng 1.
        - TrueFalse: đúng 2 phương án lần lượt "Đúng" và "Sai", đúng đúng 1.
        - FillNumber: ít nhất 2 phương án dạng số lưới (vd 12,5 hoặc -3), đúng đúng 1.
        Nội dung tiếng Việt, phù hợp chuyên đề và mức độ đã cho.
        """;

    private static string UserPrompt(string subject, string topic, GenerateAiQuestionsRequest request)
    {
        var type = request.Type switch
        {
            QuestionType.TrueFalse => "TrueFalse",
            QuestionType.FillNumber => "FillNumber",
            _ => "SingleChoice"
        };
        var level = request.Level switch
        {
            QuestionLevel.Easy => "Dễ",
            QuestionLevel.Hard => "Khó",
            _ => "Trung bình"
        };
        var hint = string.IsNullOrWhiteSpace(request.Hint) ? "(không có)" : request.Hint.Trim();
        return $"""
            Môn: {subject}
            Chuyên đề: {topic}
            Loại câu: {type}
            Mức độ: {level}
            Số câu: {request.QuestionCount}
            Gợi ý thêm: {hint}
            """;
    }

    private static bool TryParseQuestions(string raw, out List<DraftQuestion> questions, out string? error)
    {
        questions = [];
        error = null;
        var text = raw.Trim();
        if (text.StartsWith("```", StringComparison.Ordinal))
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start >= 0 && end > start)
                text = text[start..(end + 1)];
        }

        try
        {
            var payload = JsonSerializer.Deserialize<DraftPayload>(text, Json);
            questions = payload?.Questions?.Where(item => item is not null).Select(item => item!).ToList() ?? [];
            if (questions.Count == 0)
            {
                error = Messages.AiParseFailed;
                return false;
            }

            return true;
        }
        catch (JsonException)
        {
            error = Messages.AiParseFailed;
            return false;
        }
    }

    private sealed class DraftPayload
    {
        public List<DraftQuestion?>? Questions { get; set; }
    }

    private sealed class DraftQuestion
    {
        public string? Content { get; set; }
        public string? Explanation { get; set; }
        public List<DraftOption>? Options { get; set; }
    }

    private sealed class DraftOption
    {
        public string? Content { get; set; }
        public bool IsCorrect { get; set; }
    }
}
