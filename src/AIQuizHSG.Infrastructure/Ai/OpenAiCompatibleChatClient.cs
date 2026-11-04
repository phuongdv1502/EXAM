using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AIQuizHSG.Application.Ai;
using AIQuizHSG.Domain.Common;
using Microsoft.Extensions.Options;

namespace AIQuizHSG.Infrastructure.Ai;

// Gọi endpoint chat/completions kiểu OpenAI (Ollama, OpenAI, OpenRouter...).
public sealed class OpenAiCompatibleChatClient(HttpClient http, IOptions<AiOptions> options) : IAiChatClient
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public async Task<AiChatResult> CompleteAsync(IReadOnlyList<AiChatMessage> messages, CancellationToken ct)
    {
        var settings = options.Value;
        if (string.IsNullOrWhiteSpace(settings.BaseUrl) || string.IsNullOrWhiteSpace(settings.Model))
            return new AiChatResult(false, null, Messages.AiNotConfigured);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "chat/completions");
            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", settings.ApiKey.Trim());

            request.Content = new StringContent(
                JsonSerializer.Serialize(new
                {
                    model = settings.Model,
                    temperature = 0.4,
                    messages = messages.Select(item => new { role = item.Role, content = item.Content }).ToArray()
                }, Json),
                Encoding.UTF8,
                "application/json");

            using var response = await http.SendAsync(request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return new AiChatResult(false, null, string.Format(Messages.AiCallFailed, (int)response.StatusCode, Truncate(body)));

            using var document = JsonDocument.Parse(body);
            var content = document.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();
            if (string.IsNullOrWhiteSpace(content))
                return new AiChatResult(false, null, Messages.AiEmptyResponse);
            return new AiChatResult(true, content, null);
        }
        catch (TaskCanceledException) when (!ct.IsCancellationRequested)
        {
            return new AiChatResult(false, null, Messages.AiTimeout);
        }
        catch (HttpRequestException)
        {
            return new AiChatResult(false, null, Messages.AiUnreachable);
        }
        catch (Exception)
        {
            return new AiChatResult(false, null, Messages.AiCallUnexpected);
        }
    }

    private static string Truncate(string value) =>
        value.Length <= 240 ? value : value[..240] + "…";
}
