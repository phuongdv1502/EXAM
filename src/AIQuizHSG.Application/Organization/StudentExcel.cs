using System.Globalization;
using AIQuizHSG.Domain.Common;
using ClosedXML.Excel;

namespace AIQuizHSG.Application.Organization;

// Mẫu Excel học sinh. MaLop trống thì chỉ tạo hồ sơ, có mã thì xếp vào lớp của năm học đang nhập.
public static class StudentExcel
{
    public const string FileName = "mau-hoc-sinh.xlsx";
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    private static readonly string[] Headers =
        ["MaHocSinh", "HoTen", "NgaySinh", "GioiTinh", "Email", "DienThoai", "DiaChi", "MaLop"];

    public static byte[] CreateTemplate()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.AddWorksheet("HocSinh");
        for (var i = 0; i < Headers.Length; i++)
            sheet.Cell(1, i + 1).Value = Headers[i];
        sheet.Cell(2, 1).Value = "HS001";
        sheet.Cell(2, 2).Value = "Nguyễn Văn A";
        sheet.Cell(2, 3).Value = "2012-09-01";
        sheet.Cell(2, 4).Value = "Nam";
        sheet.Cell(2, 8).Value = "8A1";
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static StudentSheet Read(Stream stream)
    {
        try
        {
            using var workbook = new XLWorkbook(stream);
            var sheet = workbook.Worksheets.FirstOrDefault();
            if (sheet is null)
                return StudentSheet.Fatal(Messages.ExcelInvalid);

            var header = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in sheet.Row(1).CellsUsed())
                header[cell.GetString().Trim()] = cell.Address.ColumnNumber;
            if (Headers.Any(name => !header.ContainsKey(name)))
                return StudentSheet.Fatal(Messages.ExcelInvalid);

            var rows = new List<StudentRow>();
            var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
            for (var number = 2; number <= last; number++)
            {
                var code = Text(sheet, number, header, "MaHocSinh");
                var name = Text(sheet, number, header, "HoTen");
                if (code is null && name is null)
                    continue;
                if (code is null)
                {
                    rows.Add(StudentRow.Fail(number, Messages.StudentCodeRequired));
                    continue;
                }
                if (name is null)
                {
                    rows.Add(StudentRow.Fail(number, Messages.StudentNameRequired));
                    continue;
                }

                DateOnly? birth = null;
                var birthText = Text(sheet, number, header, "NgaySinh");
                if (birthText is not null && !TryDate(birthText, out birth))
                {
                    rows.Add(StudentRow.Fail(number, Messages.ExcelStudentInvalid));
                    continue;
                }

                Gender? gender = null;
                var genderText = Text(sheet, number, header, "GioiTinh");
                if (genderText is not null && !TryGender(genderText, out gender))
                {
                    rows.Add(StudentRow.Fail(number, Messages.ExcelStudentInvalid));
                    continue;
                }

                rows.Add(new StudentRow(
                    number,
                    OrganizationMapper.Code(code),
                    name,
                    birth,
                    gender,
                    Text(sheet, number, header, "Email"),
                    Text(sheet, number, header, "DienThoai"),
                    Text(sheet, number, header, "DiaChi"),
                    ClassCode(Text(sheet, number, header, "MaLop")),
                    null));
            }

            return new StudentSheet(null, rows);
        }
        catch (Exception)
        {
            return StudentSheet.Fatal(Messages.ExcelInvalid);
        }
    }

    private static string? ClassCode(string? value) => value is null ? null : OrganizationMapper.Code(value);

    private static string? Text(IXLWorksheet sheet, int row, Dictionary<string, int> header, string name)
    {
        var value = sheet.Cell(row, header[name]).GetString().Trim();
        return value.Length == 0 ? null : value;
    }

    private static bool TryDate(string text, out DateOnly? date)
    {
        if (DateOnly.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
            || DateOnly.TryParseExact(text, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
        {
            date = parsed;
            return true;
        }

        date = null;
        return false;
    }

    private static bool TryGender(string text, out Gender? gender)
    {
        var folded = text.Trim().ToLowerInvariant();
        gender = folded switch
        {
            "nam" or "male" => Gender.Male,
            "nữ" or "nu" or "female" => Gender.Female,
            "khác" or "khac" or "other" => Gender.Other,
            _ => null
        };
        return gender is not null;
    }
}

public sealed record StudentRow(
    int RowNumber,
    string? StudentCode,
    string? FullName,
    DateOnly? DateOfBirth,
    Gender? Gender,
    string? Email,
    string? Phone,
    string? Address,
    string? ClassCode,
    string? Error)
{
    public static StudentRow Fail(int row, string error) => new(row, null, null, null, null, null, null, null, null, error);
}

public sealed record StudentSheet(string? FatalMessage, IReadOnlyList<StudentRow> Rows)
{
    public static StudentSheet Fatal(string message) => new(message, []);
}
