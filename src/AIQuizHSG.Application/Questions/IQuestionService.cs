using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Questions;

public interface IQuestionService
{
    Task<QuestionPage> SearchAsync(QuestionFilter filter, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> GetAsync(long id, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> CreateAsync(SaveQuestionRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> UpdateAsync(long id, SaveQuestionRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> SubmitAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> ApproveAsync(long id, Actor actor, CancellationToken ct);
    Task<OperationResult<QuestionResponse>> RejectAsync(long id, RejectQuestionRequest request, Actor actor, CancellationToken ct);
    byte[] CreateImportTemplate();
    Task<OperationResult<ImportResult>> ImportAsync(Stream stream, Actor actor, CancellationToken ct);
}
