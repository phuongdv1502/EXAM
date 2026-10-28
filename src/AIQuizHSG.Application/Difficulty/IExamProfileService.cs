using AIQuizHSG.Application.Common;

namespace AIQuizHSG.Application.Difficulty;

public interface IExamProfileService
{
    Task<IReadOnlyList<ExamProfileResponse>> ListAsync(CancellationToken ct);
    Task<OperationResult<ExamProfileResponse>> GetAsync(long id, CancellationToken ct);
    Task<OperationResult<ExamProfileResponse>> CreateAsync(SaveExamProfileRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<ExamProfileResponse>> UpdateAsync(long id, SaveExamProfileRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<ProfilePreviewResponse>> PreviewAsync(long id, PreviewProfileRequest request, CancellationToken ct);
}
