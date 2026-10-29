using System.Text.Json;
using System.Text.Json.Serialization;

namespace AIQuizHSG.Application.Omr;

// Tọa độ ô là tỷ lệ 0–1 theo chiều ảnh, để cùng một mẫu dùng được cho ảnh scan và ảnh điện thoại.
public sealed class OmrTemplateConfig
{
    public int QuestionCount { get; set; }
    public int ChoiceCount { get; set; } = 4;
    public double MarkedRatio { get; set; } = 0.45;
    public double BlankRatio { get; set; } = 0.18;
    public string? Style { get; set; }

    public List<OmrBubbleConfig> Bubbles { get; set; } = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Lưới ô tô nằm dưới phần đầu phiếu, đúng khung OmrFormLayout.
    public static OmrTemplateConfig Grid(int questionCount, int choiceCount, string? style = null)
    {
        var code = OmrFormStyles.TryNormalize(style, out var normalized) ? normalized : OmrFormStyles.Khung;
        var plan = OmrFormLayout.Measure(questionCount, choiceCount, code);
        return new OmrTemplateConfig
        {
            QuestionCount = questionCount,
            ChoiceCount = choiceCount,
            Style = code,
            Bubbles = plan.Bubbles.Select(slot => new OmrBubbleConfig
            {
                QuestionNumber = slot.QuestionNumber,
                ChoiceIndex = slot.ChoiceIndex,
                X = slot.Area.X,
                Y = slot.Area.Y,
                Width = slot.Area.Width,
                Height = slot.Area.Height
            }).ToList()
        };
    }

    public string ToJson() => JsonSerializer.Serialize(this, JsonOptions);

    public static bool TryParse(string? json, out OmrTemplateConfig config)
    {
        config = new OmrTemplateConfig();
        if (string.IsNullOrWhiteSpace(json))
            return false;

        try
        {
            var parsed = JsonSerializer.Deserialize<OmrTemplateConfig>(json, JsonOptions);
            if (parsed is null || parsed.QuestionCount < 1 || parsed.ChoiceCount < 2)
                return false;
            if (parsed.Bubbles.Count != parsed.QuestionCount * parsed.ChoiceCount)
                return false;
            if (parsed.MarkedRatio <= parsed.BlankRatio || parsed.MarkedRatio >= 1 || parsed.BlankRatio < 0)
                return false;
            config = parsed;
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}

public sealed class OmrBubbleConfig
{
    public int QuestionNumber { get; set; }
    public int ChoiceIndex { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }
}
