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
    public List<OmrBubbleConfig> Bubbles { get; set; } = [];

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static OmrTemplateConfig Grid(int questionCount, int choiceCount)
    {
        var row = Math.Min(0.02, 0.7 / questionCount);
        var bubbles = new List<OmrBubbleConfig>(questionCount * choiceCount);
        for (var question = 1; question <= questionCount; question++)
        {
            for (var choice = 0; choice < choiceCount; choice++)
            {
                bubbles.Add(new OmrBubbleConfig
                {
                    QuestionNumber = question,
                    ChoiceIndex = choice,
                    X = 0.40 + choice * 0.09,
                    Y = 0.22 + (question - 1) * row,
                    Width = 0.055,
                    Height = row * 0.72
                });
            }
        }

        return new OmrTemplateConfig
        {
            QuestionCount = questionCount,
            ChoiceCount = choiceCount,
            Bubbles = bubbles
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
