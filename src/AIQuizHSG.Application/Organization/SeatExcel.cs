using ClosedXML.Excel;

namespace AIQuizHSG.Application.Organization;

// Xuất danh sách SBD theo lớp để gửi học sinh.
public static class SeatExcel
{
    public static byte[] Export(IReadOnlyList<SeatRow> rows)
    {
        using var book = new XLWorkbook();
        var sheet = book.AddWorksheet("SBD");
        sheet.Cell(1, 1).Value = "SBD";
        sheet.Cell(1, 2).Value = "MaHocSinh";
        sheet.Cell(1, 3).Value = "HoTen";
        sheet.Cell(1, 4).Value = "Lop";
        sheet.Range(1, 1, 1, 4).Style.Font.Bold = true;
        var line = 2;
        foreach (var row in rows)
        {
            sheet.Cell(line, 1).Value = row.SeatCode ?? string.Empty;
            sheet.Cell(line, 2).Value = row.StudentCode;
            sheet.Cell(line, 3).Value = row.FullName;
            sheet.Cell(line, 4).Value = row.ClassName;
            line++;
        }

        sheet.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        book.SaveAs(stream);
        return stream.ToArray();
    }
}
