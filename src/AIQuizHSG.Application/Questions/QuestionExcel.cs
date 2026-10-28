using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Questions;
using ClosedXML.Excel;

namespace AIQuizHSG.Application.Questions;

// Đọc và tạo mẫu Excel. Cột dùng mã tiếng Việt không dấu: MaMon, ChuyenDe, PhuongAn1, Dung1...
public static class QuestionExcel
{
    public const string FileName = "mau-cau-hoi.xlsx";
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly string[] Headers =
    [
        "MaMon", "ChuyenDe", "NoiDung", "MucDo", "Loai", "LoiGiai",
        "PhuongAn1", "Dung1", "PhuongAn2", "Dung2", "PhuongAn3", "Dung3",
        "PhuongAn4", "Dung4", "PhuongAn5", "Dung5", "PhuongAn6", "Dung6",
        "MucDoCauHoi"
    ];

    // Sheet CauHoi có một dòng ví dụ để người nhập biết cách điền Đúng/Sai và mức độ.
    public static byte[] CreateTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("CauHoi");
        for (var i = 0; i < Headers.Length; i++)
            sheet.Cell(1, i + 1).Value = Headers[i];

        sheet.Cell(2, 1).Value = "TIN";
        sheet.Cell(2, 2).Value = "Thuật toán";
        sheet.Cell(2, 3).Value = "Độ phức tạp của thuật toán tìm kiếm nhị phân là gì?";
        sheet.Cell(2, 4).Value = "Trung bình";
        sheet.Cell(2, 5).Value = "Một đáp án";
        sheet.Cell(2, 6).Value = "Mỗi lần chia đôi miền tìm kiếm.";
        sheet.Cell(2, 7).Value = "O(n)";
        sheet.Cell(2, 8).Value = "Sai";
        sheet.Cell(2, 9).Value = "O(log n)";
        sheet.Cell(2, 10).Value = "Đúng";
        sheet.Cell(2, 11).Value = "O(n^2)";
        sheet.Cell(2, 12).Value = "Sai";
        sheet.Cell(2, Headers.Length).Value = "Vận dụng";
        sheet.Row(1).Style.Font.Bold = true;
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    // Sai cả file thì dừng. Sai từng dòng thì ghi lỗi dòng đó và đọc tiếp các dòng còn lại.
    public static ExcelReadResult Read(Stream stream)
    {
        XLWorkbook workbook;
        try
        {
            workbook = new XLWorkbook(stream);
        }
        catch
        {
            return ExcelReadResult.Fatal(Messages.ExcelUnreadable);
        }

        using (workbook)
        {
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null)
                return ExcelReadResult.Fatal(Messages.ExcelNoSheet);

            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var lastColumn = sheet.Row(1).LastCellUsed()?.Address.ColumnNumber ?? 0;
            for (var column = 1; column <= lastColumn; column++)
            {
                var header = sheet.Cell(1, column).GetString().Trim();
                if (header.Length > 0)
                    headerMap[header] = column;
            }

            foreach (var required in new[] { "MaMon", "ChuyenDe", "NoiDung", "MucDo", "Loai", "PhuongAn1", "Dung1", "PhuongAn2", "Dung2" })
            {
                if (!headerMap.ContainsKey(required))
                    return ExcelReadResult.Fatal(string.Format(Messages.ExcelMissingColumn, required));
            }

            var result = new ExcelReadResult();
            var lastRow = sheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var row = 2; row <= lastRow; row++)
            {
                if (IsEmpty(sheet, row, lastColumn))
                    continue;

                var parsed = ParseRow(sheet, headerMap, row);
                if (parsed.Error is not null)
                    result.Errors.Add(new ImportError(row, parsed.Error));
                else
                    result.Rows.Add(parsed.Row!);
            }

            return result;
        }
    }

    private static (ParsedQuestionRow? Row, string? Error) ParseRow(IXLWorksheet sheet, Dictionary<string, int> headers, int row)
    {
        var subjectCode = Cell(sheet, row, headers, "MaMon");
        var topicName = Cell(sheet, row, headers, "ChuyenDe");
        var content = Cell(sheet, row, headers, "NoiDung");
        if (subjectCode.Length == 0 || topicName.Length == 0 || content.Length == 0)
            return (null, Messages.ExcelRequiredCells);
        if (!TryLevel(Cell(sheet, row, headers, "MucDo"), out var level))
            return (null, Messages.ExcelLevelInvalid);
        if (!TryType(Cell(sheet, row, headers, "Loai"), out var type))
            return (null, Messages.ExcelTypeInvalid);

        // Ô phương án trống thì bỏ qua. Ô có nội dung mà cột Đúng/Sai sai thì hủy cả dòng.
        var options = new List<OptionInput>();
        for (var index = 1; index <= QuestionRules.MaxOptions; index++)
        {
            if (!headers.ContainsKey($"PhuongAn{index}"))
                continue;
            var optionContent = Cell(sheet, row, headers, $"PhuongAn{index}");
            if (optionContent.Length == 0)
                continue;
            if (!headers.ContainsKey($"Dung{index}") || !TryCorrect(Cell(sheet, row, headers, $"Dung{index}"), out var isCorrect))
                return (null, string.Format(Messages.ExcelCorrectInvalid, index));
            options.Add(new OptionInput { Content = optionContent, IsCorrect = isCorrect });
        }

        var optionError = QuestionRules.ValidateOptions(type, options.Select(option => (option.Content, option.IsCorrect)).ToArray());
        if (optionError is not null)
            return (null, optionError);

        return (new ParsedQuestionRow
        {
            RowNumber = row,
            SubjectCode = subjectCode.ToUpperInvariant(),
            TopicName = topicName,
            Content = content,
            Level = level,
            Type = type,
            DifficultyText = EmptyToNull(Cell(sheet, row, headers, "MucDoCauHoi")),
            Explanation = EmptyToNull(Cell(sheet, row, headers, "LoiGiai")),
            Options = options
        }, null);
    }

    private static string Cell(IXLWorksheet sheet, int row, Dictionary<string, int> headers, string name) =>
        headers.TryGetValue(name, out var column) ? sheet.Cell(row, column).GetString().Trim() : string.Empty;

    private static bool IsEmpty(IXLWorksheet sheet, int row, int lastColumn)
    {
        for (var column = 1; column <= lastColumn; column++)
        {
            if (sheet.Cell(row, column).GetString().Trim().Length > 0)
                return false;
        }

        return true;
    }

    private static bool TryLevel(string raw, out QuestionLevel level)
    {
        switch (Key(raw))
        {
            case "de":
            case "dễ":
            case "easy":
                level = QuestionLevel.Easy;
                return true;
            case "trungbinh":
            case "trungbình":
            case "medium":
                level = QuestionLevel.Medium;
                return true;
            case "kho":
            case "khó":
            case "hard":
                level = QuestionLevel.Hard;
                return true;
            default:
                level = default;
                return false;
        }
    }

    private static bool TryType(string raw, out QuestionType type)
    {
        switch (Key(raw))
        {
            case "motdapan":
            case "mộtđápán":
            case "singlechoice":
                type = QuestionType.SingleChoice;
                return true;
            case "nhieudapan":
            case "nhiềugiáptrị":
            case "nhiềugiáptri":
            case "nhiềudapan":
            case "nhiềuđápán":
            case "multiplechoice":
                type = QuestionType.MultipleChoice;
                return true;
            default:
                type = default;
                return false;
        }
    }

    private static bool TryCorrect(string raw, out bool isCorrect)
    {
        switch (Key(raw))
        {
            case "dung":
            case "đúng":
            case "1":
            case "true":
            case "x":
            case "yes":
            case "co":
            case "có":
                isCorrect = true;
                return true;
            case "sai":
            case "0":
            case "false":
            case "no":
            case "khong":
            case "không":
                isCorrect = false;
                return true;
            default:
                isCorrect = false;
                return false;
        }
    }

    // Bỏ khoảng trắng và hạ chữ để nhận cả "Trung bình", "trungbinh" và "medium".
    private static string Key(string value) =>
        string.Concat(value.Trim().ToLowerInvariant().Where(character => !char.IsWhiteSpace(character)));

    private static string? EmptyToNull(string value) => value.Length == 0 ? null : value;
}

public sealed class ExcelReadResult
{
    public bool HasFatalError { get; private init; }
    public string? FatalMessage { get; private init; }
    public List<ParsedQuestionRow> Rows { get; } = [];
    public List<ImportError> Errors { get; } = [];

    public static ExcelReadResult Fatal(string message) => new() { HasFatalError = true, FatalMessage = message };
}

public sealed class ParsedQuestionRow
{
    public int RowNumber { get; init; }
    public string SubjectCode { get; init; } = string.Empty;
    public string TopicName { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public QuestionLevel Level { get; init; }
    public string? DifficultyText { get; init; }
    public QuestionType Type { get; init; }
    public string? Explanation { get; init; }
    public List<OptionInput> Options { get; init; } = [];
}
