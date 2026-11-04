using AIQuizHSG.Domain.Entities;

namespace AIQuizHSG.Application.Persistence;

public interface IAiQuestionRequestRepository
{
    Task AddAsync(AiQuestionRequest request, CancellationToken ct);
}
