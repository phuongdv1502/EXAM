namespace AIQuizHSG.Application.Ai;

public interface IAiChatClient
{
    // Gọi chat/completions; trả nội dung text của assistant hoặc lỗi tiếng Việt.
    Task<AiChatResult> CompleteAsync(IReadOnlyList<AiChatMessage> messages, CancellationToken ct);
}

public sealed record AiChatMessage(string Role, string Content);

public sealed record AiChatResult(bool Ok, string? Content, string? Error);
