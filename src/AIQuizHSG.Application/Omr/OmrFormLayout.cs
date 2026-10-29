namespace AIQuizHSG.Application.Omr;

// Khung phiếu A4. Phần đầu chừa cho mã QR, phần dưới xếp ô theo kiểu đã chọn.
public static class OmrFormLayout
{
    public const int PageWidth = 1200;
    public const int PageHeight = 1697;

    public readonly record struct Box(double X, double Y, double Width, double Height);

    public readonly record struct BubbleSlot(int QuestionNumber, int ChoiceIndex, Box Area);

    public readonly record struct ColumnFrame(Box Area, IReadOnlyList<int> Questions, int Gutter, int HeaderHeight, int RowHeight);

    public readonly record struct Plan(
        string Style,
        IReadOnlyList<BubbleSlot> Bubbles,
        IReadOnlyList<ColumnFrame> Columns,
        Box Header,
        Box Qr,
        int ChoiceCount);

    public static Plan Measure(int questionCount, int choiceCount, string? style = null)
    {
        var code = OmrFormStyles.TryNormalize(style, out var normalized) ? normalized : OmrFormStyles.Khung;
        var columns = code switch
        {
            OmrFormStyles.MotCot => 1,
            OmrFormStyles.Luoi => questionCount <= 8 ? 2 : questionCount <= 24 ? 4 : 5,
            _ => questionCount <= 6 ? 1 : questionCount <= 16 ? 2 : questionCount <= 30 ? 3 : 4
        };
        var maxRow = code == OmrFormStyles.Luoi ? 72 : code == OmrFormStyles.MotCot ? 96 : 112;
        return Build(questionCount, choiceCount, columns, code, maxRow, rowMajor: code == OmrFormStyles.Luoi);
    }

    public static (int Left, int Top, int Width, int Height) Pixels(Box box)
    {
        var left = (int)Math.Round(box.X * PageWidth);
        var top = (int)Math.Round(box.Y * PageHeight);
        var width = Math.Max(1, (int)Math.Round(box.Width * PageWidth));
        var height = Math.Max(1, (int)Math.Round(box.Height * PageHeight));
        return (left, top, width, height);
    }

    private static Plan Build(int questionCount, int choiceCount, int columns, string style, int maxRow, bool rowMajor)
    {
        var rows = (int)Math.Ceiling(questionCount / (double)columns);
        const int margin = 48;
        const int tableTop = 530;
        const int bottom = 1620;
        const int headerRow = 52;
        const int gap = 22;
        var rowHeight = Math.Min(maxRow, Math.Max(44, (bottom - tableTop - headerRow) / rows));
        var bubble = Math.Min(34, rowHeight - 16);
        var tableWidth = PageWidth - margin * 2;
        var columnWidth = (tableWidth - gap * (columns - 1)) / columns;
        var gutter = (int)Math.Min(style == OmrFormStyles.Luoi ? 56 : 78, columnWidth / (choiceCount + 2));
        var cell = (columnWidth - gutter) / (double)choiceCount;
        if (bubble > cell - 12)
            bubble = Math.Max(16, (int)cell - 12);

        var frames = new List<ColumnFrame>();
        var bubbles = new List<BubbleSlot>();
        for (var column = 0; column < columns; column++)
        {
            var questions = new int[rows];
            var left = margin + column * (columnWidth + gap);
            var height = headerRow + rows * rowHeight;
            for (var row = 0; row < rows; row++)
            {
                var question = rowMajor ? row * columns + column + 1 : column * rows + row + 1;
                if (question > questionCount)
                    continue;
                questions[row] = question;
                for (var choice = 0; choice < choiceCount; choice++)
                {
                    var cellLeft = left + gutter + choice * cell;
                    var bubbleLeft = (int)Math.Round(cellLeft + (cell - bubble) / 2);
                    var bubbleTop = tableTop + headerRow + row * rowHeight + (rowHeight - bubble) / 2;
                    bubbles.Add(new BubbleSlot(question, choice, BoxOf(bubbleLeft, bubbleTop, bubble, bubble)));
                }
            }

            if (questions.Any(item => item > 0))
                frames.Add(new ColumnFrame(BoxOf(left, tableTop, columnWidth, height), questions, gutter, headerRow, rowHeight));
        }

        return new Plan(
            style,
            bubbles,
            frames,
            BoxOf(margin, 36, PageWidth - margin * 2, 460),
            BoxOf(76, 86, 230, 230),
            choiceCount);
    }

    private static Box BoxOf(int left, int top, int width, int height) =>
        new(left / (double)PageWidth, top / (double)PageHeight, width / (double)PageWidth, height / (double)PageHeight);
}
