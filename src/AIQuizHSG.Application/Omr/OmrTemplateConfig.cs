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

    public List<OmrSectionConfig> Sections { get; set; } = [];

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

    // Ít nhất hai phần có câu. Số câu tối đa theo mẫu phiếu: 40, 8 và 6.
    public static bool TryFromParts(IReadOnlyList<(string Kind, int QuestionCount)> parts, out OmrTemplateConfig config)
    {
        config = new OmrTemplateConfig();
        if (parts.Count == 0)
            return false;

        var sections = new List<OmrSectionConfig>();
        foreach (var part in parts)
        {
            if (part.QuestionCount == 0)
                continue;
            if (!OmrSectionKinds.TryNormalize(part.Kind, out var kind) || !OmrSectionKinds.CountAllowed(kind, part.QuestionCount))
                return false;
            sections.Add(OmrSectionConfig.Create(kind, part.QuestionCount));
        }

        if (sections.Count < 2)
            return false;

        var plan = OmrFormLayout.MeasureParts(sections);
        var total = sections.Sum(item => item.ItemCount);
        if (total < 1 || total > 80 || plan.Bubbles.Count == 0)
            return false;

        config = new OmrTemplateConfig
        {
            QuestionCount = total,
            ChoiceCount = plan.ChoiceCount,
            Style = OmrFormStyles.NhieuPhan,
            Sections = sections,
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
        return true;
    }

    // Phiếu kiểm tra: phần I bắt buộc, phần II đúng/sai có thể bỏ. Không có phần điền số.
    public static bool TryFromCheck(IReadOnlyList<(string Kind, int QuestionCount)> parts, out OmrTemplateConfig config)
    {
        config = new OmrTemplateConfig();
        var kept = parts.Where(item => item.QuestionCount > 0).ToList();
        if (kept.Count is < 1 or > 2)
            return false;
        if (!OmrSectionKinds.TryNormalize(kept[0].Kind, out var first) || first != OmrSectionKinds.BonDapAn
            || !OmrSectionKinds.CheckCountAllowed(first, kept[0].QuestionCount))
            return false;
        if (kept.Count == 2
            && (!OmrSectionKinds.TryNormalize(kept[1].Kind, out var second) || second != OmrSectionKinds.DungSai
                || !OmrSectionKinds.CheckCountAllowed(second, kept[1].QuestionCount)))
            return false;

        var sections = kept.Select(item => OmrSectionConfig.Create(
            item.Kind.Equals(OmrSectionKinds.DungSai, StringComparison.OrdinalIgnoreCase) ? OmrSectionKinds.DungSai : OmrSectionKinds.BonDapAn,
            item.QuestionCount)).ToList();
        var plan = OmrFormLayout.MeasureCheck(sections);
        var total = sections.Sum(item => item.ItemCount);
        if (total < 1 || plan.Bubbles.Count == 0)
            return false;

        config = new OmrTemplateConfig
        {
            QuestionCount = total,
            ChoiceCount = 4,
            Style = OmrFormStyles.KiemTra,
            Sections = sections,
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
        return true;
    }

    // Phiếu có tự luận: phần I bắt buộc, phần II và phần III có thể bỏ, đúng thứ tự.
    public static bool TryFromEssay(IReadOnlyList<(string Kind, int QuestionCount)> parts, out OmrTemplateConfig config)
    {
        config = new OmrTemplateConfig();
        var kept = parts.Where(item => item.QuestionCount > 0).ToList();
        if (kept.Count is < 1 or > 3)
            return false;

        var expected = new[] { OmrSectionKinds.BonDapAn, OmrSectionKinds.DungSai, OmrSectionKinds.DienSo };
        var sections = new List<OmrSectionConfig>();
        for (var index = 0; index < kept.Count; index++)
        {
            if (!OmrSectionKinds.TryNormalize(kept[index].Kind, out var kind) || kind != expected[index]
                || !OmrSectionKinds.EssayCountAllowed(kind, kept[index].QuestionCount))
                return false;
            sections.Add(OmrSectionConfig.Create(kind, kept[index].QuestionCount));
        }

        var plan = OmrFormLayout.MeasureEssay(sections);
        var total = sections.Sum(item => item.ItemCount);
        if (total < 1 || plan.Bubbles.Count == 0)
            return false;

        config = new OmrTemplateConfig
        {
            QuestionCount = total,
            ChoiceCount = 4,
            Style = OmrFormStyles.TuLuan,
            Sections = sections,
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
        return true;
    }

    public string KindOf(int questionNumber)
    {
        var cursor = 1;
        foreach (var section in Sections)
        {
            if (questionNumber >= cursor && questionNumber < cursor + section.ItemCount)
                return section.Kind;
            cursor += section.ItemCount;
        }

        return string.Empty;
    }

    // Phần bốn đáp án cần đúng 4 phương án, đúng/sai cần 2. Điền số chỉ cần có phương án để đối chiếu chữ số.
    public bool OptionCountsFit(IReadOnlyList<int> optionCounts)
    {
        if (Sections.Count == 0)
            return true;
        if (optionCounts.Count != QuestionCount)
            return false;

        var index = 0;
        foreach (var section in Sections)
        {
            for (var item = 0; item < section.ItemCount; item++)
            {
                var count = optionCounts[index++];
                var fits = section.Kind switch
                {
                    OmrSectionKinds.BonDapAn => count == 4,
                    OmrSectionKinds.DungSai => count == 2,
                    OmrSectionKinds.DienSo => count >= 2,
                    _ => false
                };
                if (!fits)
                    return false;
            }
        }

        return true;
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
            parsed.Sections ??= [];
            parsed.Bubbles ??= [];
            if (parsed.Sections.Count == 0)
            {
                if (parsed.Bubbles.Count != parsed.QuestionCount * parsed.ChoiceCount)
                    return false;
            }
            else if (!SectionsMatch(parsed))
            {
                return false;
            }
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

    private static bool SectionsMatch(OmrTemplateConfig config)
    {
        if (config.Style == OmrFormStyles.KiemTra)
            return CheckSections(config);
        if (config.Style == OmrFormStyles.TuLuan)
            return EssaySections(config);
        if (config.Style != OmrFormStyles.NhieuPhan || config.Sections.Count < 2)
            return false;

        return MarksMatch(config);
    }

    // Phần I là bốn đáp án. Phần II, nếu có, là đúng/sai và đứng ngay sau.
    private static bool CheckSections(OmrTemplateConfig config)
    {
        if (config.Sections.Count is < 1 or > 2)
            return false;
        if (!OmrSectionKinds.TryNormalize(config.Sections[0].Kind, out var first) || first != OmrSectionKinds.BonDapAn
            || !OmrSectionKinds.CheckCountAllowed(first, config.Sections[0].QuestionCount))
            return false;
        config.Sections[0].Kind = first;
        if (config.Sections.Count == 2)
        {
            if (!OmrSectionKinds.TryNormalize(config.Sections[1].Kind, out var second) || second != OmrSectionKinds.DungSai
                || !OmrSectionKinds.CheckCountAllowed(second, config.Sections[1].QuestionCount))
                return false;
            config.Sections[1].Kind = second;
        }

        return MarksMatch(config);
    }

    // Phần I bốn đáp án, rồi đúng/sai, rồi điền số. Mỗi phần tôn trọng số câu của phiếu tự luận.
    private static bool EssaySections(OmrTemplateConfig config)
    {
        if (config.Sections.Count is < 1 or > 3)
            return false;
        var expected = new[] { OmrSectionKinds.BonDapAn, OmrSectionKinds.DungSai, OmrSectionKinds.DienSo };
        for (var index = 0; index < config.Sections.Count; index++)
        {
            if (!OmrSectionKinds.TryNormalize(config.Sections[index].Kind, out var kind) || kind != expected[index]
                || !OmrSectionKinds.EssayCountAllowed(kind, config.Sections[index].QuestionCount))
                return false;
            config.Sections[index].Kind = kind;
        }

        return MarksMatch(config);
    }

    private static bool MarksMatch(OmrTemplateConfig config)
    {
        var expected = 0;
        var items = 0;
        foreach (var section in config.Sections)
        {
            if (!OmrSectionKinds.TryNormalize(section.Kind, out var kind))
                return false;
            var allowed = config.Style switch
            {
                OmrFormStyles.KiemTra => OmrSectionKinds.CheckCountAllowed(kind, section.QuestionCount),
                OmrFormStyles.TuLuan => OmrSectionKinds.EssayCountAllowed(kind, section.QuestionCount),
                _ => OmrSectionKinds.CountAllowed(kind, section.QuestionCount)
            };
            if (!allowed)
                return false;
            section.Kind = kind;
            section.ChoiceCount = OmrSectionKinds.BubbleChoices(kind);
            section.ItemCount = section.QuestionCount * OmrSectionKinds.ItemsPerQuestion(kind);
            items += section.ItemCount;
            var digitBubbles = config.Style is OmrFormStyles.TuLuan or OmrFormStyles.NhieuPhan
                ? OmrSectionKinds.EssayDigitBubbles
                : section.ChoiceCount;
            expected += section.Kind == OmrSectionKinds.DienSo
                ? section.QuestionCount * digitBubbles
                : section.ItemCount * section.ChoiceCount;
        }

        if (items != config.QuestionCount || config.Bubbles.Count != expected)
            return false;

        for (var number = 1; number <= items; number++)
        {
            var kind = config.KindOf(number);
            var marks = config.Bubbles.Where(item => item.QuestionNumber == number).OrderBy(item => item.ChoiceIndex).ToList();
            // Phiếu Bộ và phiếu tự luận không tô đủ lưới 4×12, chỉ các ô có trên mẫu.
            if (kind == OmrSectionKinds.DienSo && config.Style is OmrFormStyles.TuLuan or OmrFormStyles.NhieuPhan)
            {
                var indexes = Enumerable.Range(0, OmrDigitGrid.Columns)
                    .SelectMany(column => Enumerable.Range(0, OmrDigitGrid.Rows)
                        .Where(row => OmrSectionKinds.EssayDigitCell(column, row))
                        .Select(row => OmrDigitGrid.Index(column, row)))
                    .OrderBy(index => index)
                    .ToArray();
                if (marks.Count != indexes.Length)
                    return false;
                for (var index = 0; index < indexes.Length; index++)
                {
                    if (marks[index].ChoiceIndex != indexes[index] || marks[index].Width <= 0 || marks[index].Height <= 0)
                        return false;
                }

                continue;
            }

            var choices = kind == OmrSectionKinds.DungSai ? 2 : kind == OmrSectionKinds.DienSo ? OmrSectionKinds.BubbleChoices(kind) : 4;
            if (marks.Count != choices)
                return false;
            for (var choice = 0; choice < marks.Count; choice++)
            {
                if (marks[choice].ChoiceIndex != choice || marks[choice].Width <= 0 || marks[choice].Height <= 0)
                    return false;
            }
        }

        return true;
    }
}

public sealed class OmrSectionConfig
{
    public string Kind { get; set; } = OmrSectionKinds.BonDapAn;
    public int QuestionCount { get; set; }
    public int ChoiceCount { get; set; }
    public int ItemCount { get; set; }

    public static OmrSectionConfig Create(string kind, int questionCount) => new()
    {
        Kind = kind,
        QuestionCount = questionCount,
        ChoiceCount = OmrSectionKinds.BubbleChoices(kind),
        ItemCount = questionCount * OmrSectionKinds.ItemsPerQuestion(kind)
    };

    public static int ShownChoices(OmrSectionConfig section) =>
        section.Kind == OmrSectionKinds.BonDapAn ? 4 : 2;
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
