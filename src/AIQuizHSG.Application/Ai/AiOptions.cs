namespace AIQuizHSG.Application.Ai;

// Cấu hình LLM tương thích OpenAI. Mặc định trỏ Ollama local (miễn phí).
public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public string BaseUrl { get; set; } = "http://127.0.0.1:11434/v1";
    public string Model { get; set; } = "qwen2.5:7b";
    // Ollama chấp nhận mọi chuỗi; OpenAI cần sk-...
    public string ApiKey { get; set; } = "ollama";
    public int TimeoutSeconds { get; set; } = 120;
}
