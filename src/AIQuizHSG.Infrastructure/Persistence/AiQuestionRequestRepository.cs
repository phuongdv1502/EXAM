using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Entities;
using AIQuizHSG.Infrastructure.Data;

namespace AIQuizHSG.Infrastructure.Persistence;

public sealed class AiQuestionRequestRepository(AiQuizDbContext db) : IAiQuestionRequestRepository
{
    public Task AddAsync(AiQuestionRequest request, CancellationToken ct) =>
        db.AiQuestionRequests.AddAsync(request, ct).AsTask();
}
