namespace AIQuizHSG.Application.Omr;

// Khung phiếu A4. Phần đầu chừa cho mã QR, phần dưới là các cột ô tô.
public static class OmrFormLayout
{
    public const int PageWidth = 1200;
    public const int PageHeight = 1697;

    public readonly record struct Box(double X, double Y, double Width, double Height);

    public readonly record struct BubbleSlot(int QuestionNumber, int ChoiceIndex, Box Area);

    public readonly record struct ColumnFrame(Box Area, int FirstQuestion, int LastQuestion, int Gutter, int HeaderHeight, int RowHeight);

    public readonly record struct Plan(
        IReadOnlyList<BubbleSlot> Bubbles,
        IReadOnlyList<ColumnFrame> Columns,
        Box Header,
        Box Qr,
        int ChoiceCount);

    public static Plan Measure(int questionCount, int choiceCount)
    {
        var columns = questionCount <= 6 ? 1 : questionCount <= 16 ? 2 : questionCount <= 30 ? 3 : 4;
        var rows = (int)Math.Ceiling(questionCount / (double)columns);
        const int margin = 48;
        const int tableTop = 530;
        const int bottom = 1620;
        const int headerRow = 52;
        const int gap = 22;
        var rowHeight = Math.Min(112, Math.Max(44, (bottom - tableTop - headerRow) / rows));
        var bubble = Math.Min(34, rowHeight - 16);
        var tableWidth = PageWidth - margin * 2;
        var columnWidth = (tableWidth - gap * (columns - 1)) / columns;
        var gutter = Math.Min(78, columnWidth / (choiceCount + 2));
        var cell = (columnWidth - gutter) / (double)choiceCount;
        if (bubble > cell - 12)
            bubble = Math.Max(16, (int)cell - 12);

        var frames = new List<ColumnFrame>();
        var bubbles = new List<BubbleSlot>();
        for (var column = 0; column < columns; column++)
        {
            var first = column * rows + 1;
            if (first > questionCount)
                break;
            var last = Math.Min(questionCount, first + rows - 1);
            var usedRows = last - first + 1;
            var left = margin + column * (columnWidth + gap);
            var height = headerRow + usedRows * rowHeight;
            frames.Add(new ColumnFrame(BoxOf(left, tableTop, columnWidth, height), first, last, gutter, headerRow, rowHeight));

            for (var row = 0; row < usedRows; row++)
            {
                var question = first + row;
                for (var choice = 0; choice < choiceCount; choice++)
                {
                    var cellLeft = left + gutter + choice * cell;
                    var bubbleLeft = (int)Math.Round(cellLeft + (cell - bubble) / 2);
                    var bubbleTop = tableTop + headerRow + row * rowHeight + (rowHeight - bubble) / 2;
                    bubbles.Add(new BubbleSlot(question, choice, BoxOf(bubbleLeft, bubbleTop, bubble, bubble)));
                }
            }
        }

        return new Plan(
            bubbles,
            frames,
            BoxOf(margin, 36, PageWidth - margin * 2, 460),
            BoxOf(76, 86, 230, 230),
            choiceCount);
    }

    public static (int Left, int Top, int Width, int Height) Pixels(Box box)
    {
        var left = (int)Math.Round(box.X * PageWidth);
        var top = (int)Math.Round(box.Y * PageHeight);
        var width = Math.Max(1, (int)Math.Round(box.Width * PageWidth));
        var height = Math.Max(1, (int)Math.Round(box.Height * PageHeight));
        return (left, top, width, height);
    }

    private static Box BoxOf(int left, int top, int width, int height) =>
        new(left / (double)PageWidth, top / (double)PageHeight, width / (double)PageWidth, height / (double)PageHeight);
}
