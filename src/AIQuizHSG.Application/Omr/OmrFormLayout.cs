namespace AIQuizHSG.Application.Omr;

// Khung phiếu A4. Phần đầu chừa cho mã QR, phần dưới xếp ô theo kiểu đã chọn.
public static class OmrFormLayout
{
    public const int PageWidth = 1200;
    public const int PageHeight = 1697;

    public readonly record struct Box(double X, double Y, double Width, double Height);

    public readonly record struct BubbleSlot(int QuestionNumber, int ChoiceIndex, Box Area);

    public readonly record struct ColumnFrame(Box Area, IReadOnlyList<int> Questions, int Gutter, int HeaderHeight, int RowHeight, int ChoiceCount, string ChoiceKind);

    public readonly record struct SectionTitle(Box Area, string Text);

    public readonly record struct TruthRow(int QuestionNumber, string Label);

    public readonly record struct TruthBlock(Box Area, string Title, int LabelWidth, int HeaderHeight, int RowHeight, IReadOnlyList<TruthRow> Rows);

    public readonly record struct DigitBlock(Box Area, string Title, int QuestionNumber, int LabelWidth, int HeaderHeight, int Cell);

    public readonly record struct LineField(string Label, Box Area);

    public readonly record struct CodeBubble(int Column, int Digit, Box Area);

    public readonly record struct CodeGrid(string Title, Box Area, int Columns, int LabelWidth, int HeaderHeight, int Cell, IReadOnlyList<CodeBubble> Bubbles);

    public readonly record struct Plan(
        string Style,
        IReadOnlyList<BubbleSlot> Bubbles,
        IReadOnlyList<ColumnFrame> Columns,
        Box Header,
        Box Qr,
        int ChoiceCount,
        IReadOnlyList<SectionTitle> Sections,
        IReadOnlyList<TruthBlock> Truths,
        IReadOnlyList<DigitBlock> Digits,
        IReadOnlyList<LineField> Fields,
        IReadOnlyList<CodeGrid> Codes);

    public static Plan Measure(int questionCount, int choiceCount, string? style = null)
    {
        var code = OmrFormStyles.TryNormalize(style, out var normalized) ? normalized : OmrFormStyles.Khung;
        if (code == OmrFormStyles.Khung)
            return BuildKhung(questionCount, choiceCount);
        if (code == OmrFormStyles.PhieuThi)
            return BuildPhieuThi(questionCount, choiceCount);
        var columns = code switch
        {
            OmrFormStyles.MotCot => 1,
            OmrFormStyles.Luoi => questionCount <= 8 ? 2 : questionCount <= 24 ? 4 : 5,
            _ => questionCount <= 6 ? 1 : questionCount <= 16 ? 2 : questionCount <= 30 ? 3 : 4
        };
        var maxRow = code == OmrFormStyles.Luoi ? 72 : code == OmrFormStyles.MotCot ? 96 : 112;
        return Build(questionCount, choiceCount, columns, code, maxRow, rowMajor: code == OmrFormStyles.Luoi);
    }

    public static Plan For(OmrTemplateConfig config) =>
        config.Style == OmrFormStyles.KiemTra
            ? MeasureCheck(config.Sections)
            : config.Style == OmrFormStyles.TuLuan
                ? MeasureEssay(config.Sections)
                : config.Sections.Count > 0
                    ? MeasureParts(config.Sections)
                    : Measure(config.QuestionCount, config.ChoiceCount, config.Style);

    // Phiếu kiểm tra A4: phần I mỗi cột mười câu, phần II là bảng đúng/sai. Kích thước lấy từ phiếu mẫu.
    public static Plan MeasureCheck(IReadOnlyList<OmrSectionConfig> sections)
    {
        var partOne = sections.FirstOrDefault(item => item.Kind == OmrSectionKinds.BonDapAn)?.QuestionCount ?? 0;
        var partTwo = sections.FirstOrDefault(item => item.Kind == OmrSectionKinds.DungSai)?.QuestionCount ?? 0;
        return BuildKiemTra(partOne, partTwo);
    }

    // Phiếu có phần tự luận: tọa độ lấy từ phiếu mẫu 550×770, giãn lên trang A4.
    public static Plan MeasureEssay(IReadOnlyList<OmrSectionConfig> sections)
    {
        var partOne = sections.FirstOrDefault(item => item.Kind == OmrSectionKinds.BonDapAn)?.QuestionCount ?? 0;
        var partTwo = sections.FirstOrDefault(item => item.Kind == OmrSectionKinds.DungSai)?.QuestionCount ?? 0;
        var partThree = sections.FirstOrDefault(item => item.Kind == OmrSectionKinds.DienSo)?.QuestionCount ?? 0;
        return BuildTuLuan(partOne, partTwo, partThree);
    }

    // Phần I xếp cột tối đa 10 câu. Phần II mỗi câu bốn ý. Phần III là lưới chữ số.
    public static Plan MeasureParts(IReadOnlyList<OmrSectionConfig> sections)
    {
        const int left = 36;
        const int width = PageWidth - 72;
        var titles = new List<SectionTitle>();
        var columns = new List<ColumnFrame>();
        var truths = new List<TruthBlock>();
        var digits = new List<DigitBlock>();
        var bubbles = new List<BubbleSlot>();
        var question = 1;
        var identity = MinistryIdentity();
        var y = (int)Math.Round((identity.Header.Y + identity.Header.Height) * PageHeight) + 36;

        foreach (var section in sections)
        {
            if (section.QuestionCount < 1)
                continue;
            if (section.Kind == OmrSectionKinds.BonDapAn)
            {
                titles.Add(new SectionTitle(BoxOf(left, y, width, 28), "PHẦN I"));
                y += 34;
                y = AddChoiceColumns(columns, bubbles, ref question, section.QuestionCount, left, y, width);
            }
            else if (section.Kind == OmrSectionKinds.DungSai)
            {
                titles.Add(new SectionTitle(BoxOf(left, y, width, 28), "PHẦN II"));
                y += 34;
                y = AddTruth(truths, bubbles, ref question, section.QuestionCount, left, y, width);
            }
            else if (section.Kind == OmrSectionKinds.DienSo)
            {
                titles.Add(new SectionTitle(BoxOf(left, y, width, 28), "PHẦN III"));
                y += 34;
                y = AddDigits(digits, bubbles, ref question, section.QuestionCount, left, y, width);
            }

            y += 12;
        }

        var choiceCount = sections.Where(item => item.Kind != OmrSectionKinds.DienSo).Select(OmrSectionConfig.ShownChoices).DefaultIfEmpty(4).Max();
        return new Plan(
            OmrFormStyles.NhieuPhan,
            bubbles,
            columns,
            identity.Header,
            identity.Qr,
            choiceCount,
            titles,
            truths,
            digits,
            identity.Fields,
            identity.Codes);
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
                frames.Add(new ColumnFrame(BoxOf(left, tableTop, columnWidth, height), questions, gutter, headerRow, rowHeight, choiceCount, OmrSectionKinds.BonDapAn));
        }

        return new Plan(
            style,
            bubbles,
            frames,
            BoxOf(margin, 36, PageWidth - margin * 2, 460),
            BoxOf(76, 86, 230, 230),
            choiceCount,
            [],
            [],
            [],
            [],
            []);
    }

    private const double MinistryPdfWidth = 589.6063232421875;
    private const double MinistryPdfHeight = 841.8897705078125;

    private static int MinistryPx(double value) => (int)Math.Round(value * PageWidth / MinistryPdfWidth);

    private static int MinistryPy(double value) => (int)Math.Round(value * PageHeight / MinistryPdfHeight);

    // Phần I lấy khổ cột từ phiếu Bộ: bốn cột cách đều, mỗi cột tối đa mười câu.
    private static int AddChoiceColumns(List<ColumnFrame> frames, List<BubbleSlot> bubbles, ref int question, int questionCount, int left, int top, int width)
    {
        _ = left;
        _ = width;
        var origin = MinistryPx(34.3);
        var step = MinistryPx(133.3);
        var columnWidth = MinistryPx(113.4);
        var headerRow = MinistryPy(17.8);
        var rowPitch = MinistryPy(14.2);
        var bubble = MinistryPx(8.4);
        var first = MinistryPx(25.5);
        var choicePitch = MinistryPx(23.6);
        var columns = Math.Clamp((int)Math.Ceiling(questionCount / 10d), 1, 4);
        var rows = (int)Math.Ceiling(questionCount / (double)columns);
        var height = headerRow + Math.Max(0, rows - 1) * rowPitch + bubble + MinistryPy(5.3);
        var start = question;

        for (var column = 0; column < columns; column++)
        {
            var questions = new int[rows];
            var x = origin + column * step;
            for (var row = 0; row < rows; row++)
            {
                var number = start + column * rows + row;
                if (number >= start + questionCount)
                    continue;
                questions[row] = number;
                for (var choice = 0; choice < 4; choice++)
                {
                    var bubbleLeft = x + first + choice * choicePitch;
                    var bubbleTop = top + headerRow + row * rowPitch;
                    bubbles.Add(new BubbleSlot(number, choice, BoxOf(bubbleLeft, bubbleTop, bubble, bubble)));
                }
            }

            if (questions.Any(item => item > 0))
                frames.Add(new ColumnFrame(BoxOf(x, top, columnWidth, height), questions, first, headerRow, rowPitch, 4, OmrSectionKinds.BonDapAn));
        }

        question += questionCount;
        return top + height;
    }

    // Phần II: bốn ô, mỗi ô hai câu đúng/sai đặt cạnh nhau như phiếu Bộ.
    private static int AddTruth(List<TruthBlock> blocks, List<BubbleSlot> bubbles, ref int question, int questionCount, int left, int top, int width)
    {
        _ = left;
        _ = width;
        // Một khung cho hai câu, vạch giữa đúng một nửa và hai bên rộng bằng nhau.
        var origin = MinistryPx(34.3);
        var step = MinistryPx(133.3);
        var pairWidth = MinistryPx(113.4);
        var leftWidth = pairWidth / 2;
        var rightWidth = pairWidth - leftWidth;
        var header = MinistryPy(32);
        var rowPitch = MinistryPy(16);
        var bubble = MinistryPx(8.4);
        var blockHeight = header + 3 * rowPitch + bubble + MinistryPy(8);
        const int across = 4;
        const int label = 26;
        const int choicePitch = 44;
        var pairs = (int)Math.Ceiling(questionCount / 2d);
        var rows = (int)Math.Ceiling(pairs / (double)across);

        for (var index = 0; index < questionCount; index++)
        {
            var pair = index / 2;
            var side = index % 2;
            var row = pair / across;
            var column = pair % across;
            var boxX = origin + column * step;
            var y = top + row * (blockHeight + MinistryPy(8));
            var halfWidth = side == 0 ? leftWidth : rightWidth;
            var halfLeft = boxX + side * leftWidth;
            var alone = questionCount == index + 1 && side == 0;
            var sideWidth = alone ? pairWidth : halfWidth;
            var content = label + choicePitch + bubble;
            var inset = Math.Max(6, ((alone ? leftWidth : halfWidth) - content) / 2);
            var lines = new List<TruthRow>();
            for (var item = 0; item < 4; item++)
            {
                lines.Add(new TruthRow(question, ((char)('a' + item)).ToString()));
                for (var choice = 0; choice < 2; choice++)
                {
                    var bubbleLeft = halfLeft + inset + label + choice * choicePitch;
                    var bubbleTop = y + header + item * rowPitch;
                    bubbles.Add(new BubbleSlot(question, choice, BoxOf(bubbleLeft, bubbleTop, bubble, bubble)));
                }

                question++;
            }

            blocks.Add(new TruthBlock(BoxOf(halfLeft, y, sideWidth, blockHeight), $"Câu {index + 1}", label, header, rowPitch, lines));
        }

        return top + rows * blockHeight + Math.Max(0, rows - 1) * MinistryPy(8);
    }

    private static int AddDigits(List<DigitBlock> blocks, List<BubbleSlot> bubbles, ref int question, int questionCount, int left, int top, int width)
    {
        _ = left;
        _ = width;
        // Dấu trừ một ô, dấu phẩy hai ô giữa, chữ số đủ bốn cột, như phiếu Bộ.
        var origin = MinistryPx(35);
        var blockWidth = MinistryPx(85);
        var blockHeight = MinistryPy(212.6);
        var header = MinistryPy(23.3);
        var rowPitch = MinistryPy(16);
        var bubble = MinistryPx(8.2);
        var columnOffset = new[] { 21.7, 35.9, 50.0, 64.2 };
        const int perRow = 6;
        var rows = (int)Math.Ceiling(questionCount / (double)perRow);

        for (var index = 0; index < questionCount; index++)
        {
            var row = index / perRow;
            var column = index % perRow;
            var x = origin + column * blockWidth;
            var y = top + row * blockHeight;
            for (var digitRow = 0; digitRow < OmrDigitGrid.Rows; digitRow++)
            {
                for (var digitColumn = 0; digitColumn < OmrDigitGrid.Columns; digitColumn++)
                {
                    if (!OmrSectionKinds.EssayDigitCell(digitColumn, digitRow))
                        continue;
                    var bubbleLeft = x + MinistryPx(columnOffset[digitColumn]);
                    var bubbleTop = y + header + digitRow * rowPitch;
                    bubbles.Add(new BubbleSlot(question, OmrDigitGrid.Index(digitColumn, digitRow), BoxOf(bubbleLeft, bubbleTop, bubble, bubble)));
                }
            }

            blocks.Add(new DigitBlock(BoxOf(x, y, blockWidth, blockHeight), $"Câu {index + 1}", question, MinistryPx(16), header, rowPitch));
            question++;
        }

        return top + rows * blockHeight;
    }

    private const int SheetMargin = 32;
    private const int TableTop = 96;
    private const int TableWidth = PageWidth - SheetMargin * 2;
    private const int QrColumn = 128;
    private const int CodeCell = 16;
    private const int CodeLabel = 16;
    private const int CodeHead = 14;
    private const int CodeBubbleSize = 11;
    private const int TitleStrip = 16;
    private const int GridHeight = CodeHead + 10 * CodeCell;
    private const int TableHeight = TitleStrip + GridHeight + 4;
    private const int HeaderBottom = TableTop + TableHeight;

    // Phiếu thi: chỉ vẽ đúng số câu của mẫu. Mở thêm cột khi một cột không còn vừa trang.
    private static Plan BuildPhieuThi(int questionCount, int choiceCount)
    {
        const int rowsThatFit = 30;
        var columns = Math.Clamp((int)Math.Ceiling(questionCount / (double)rowsThatFit), 1, 4);
        var rows = (int)Math.Ceiling(questionCount / (double)columns);

        const int tableTop = 118;
        const int cell = 17;
        const int codeLabel = 18;
        const int codeHead = 16;
        var codeHeight = codeHead + 10 * cell;
        var madeWidth = codeLabel + 3 * cell;
        var sbdWidth = codeLabel + 6 * cell;
        var madeX = PageWidth - 36 - madeWidth;
        var sbdX = madeX - 12 - sbdWidth;
        const int proctorX = 28;
        const int proctorW = 168;
        var fieldX = proctorX + proctorW;
        var fieldW = sbdX - 12 - fieldX;
        var half = codeHeight / 2;

        var fields = new List<LineField>
        {
            new("Kỳ thi:", BoxOf(360, 54, 460, 16)),
            new("Ngày thi:", BoxOf(860, 54, 250, 16)),
            new("Bài thi:", BoxOf(108, 80, 680, 16)),
            new("Cán bộ coi thi 1", BoxOf(proctorX, tableTop, proctorW, half)),
            new("Cán bộ coi thi 2", BoxOf(proctorX, tableTop + half, proctorW, codeHeight - half))
        };
        var labels = new[]
        {
            "1. Hội đồng thi:",
            "2. Điểm thi:",
            "3. Phòng thi:",
            "4. Họ và tên thí sinh:",
            "5. Ngày sinh:",
            "6. Chữ ký của thí sinh:"
        };
        var lineStep = (codeHeight - 18) / labels.Length;
        for (var index = 0; index < labels.Length; index++)
            fields.Add(new LineField(labels[index], BoxOf(fieldX + 10, tableTop + 8 + index * lineStep, fieldW - 22, 16)));

        var codes = new List<CodeGrid>
        {
            SmallCode("7. Số báo danh", sbdX, tableTop, 6, cell, codeLabel, codeHead),
            SmallCode("8. Mã đề thi", madeX, tableTop, 3, cell, codeLabel, codeHead)
        };

        var noteY = tableTop + codeHeight + 14;
        var sections = new List<SectionTitle>
        {
            new(BoxOf(140, noteY, 920, 18), "Chú ý: Thí sinh cần đọc kỹ hướng dẫn ở mặt sau Phiếu này.")
        };

        const int groupGap = 8;
        const int bubble = 14;
        const int gutter = 32;
        const int pitch = 20;
        var top = noteY + 30;
        var gaps = Math.Max(0, rows - 1) / 5 * groupGap;
        var rowHeight = (int)Math.Clamp(Math.Floor((1648 - top - gaps) / (double)Math.Max(rows, 1)), 28, 42);
        const int left = 56;
        var slot = (PageWidth - left - 40) / (double)columns;
        var frames = new List<ColumnFrame>();
        var bubbles = new List<BubbleSlot>();
        for (var column = 0; column < columns; column++)
        {
            var count = Math.Min(rows, questionCount - column * rows);
            var questions = new int[count];
            var x = (int)Math.Round(left + column * slot);
            for (var line = 0; line < count; line++)
            {
                var number = column * rows + line + 1;
                questions[line] = number;
                var y = top + line * rowHeight + line / 5 * groupGap;
                for (var choice = 0; choice < choiceCount; choice++)
                {
                    var bubbleLeft = x + gutter + choice * pitch;
                    var bubbleTop = y + (rowHeight - bubble) / 2;
                    bubbles.Add(new BubbleSlot(number, choice, BoxOf(bubbleLeft, bubbleTop, bubble, bubble)));
                }
            }

            var height = count * rowHeight + Math.Max(0, count - 1) / 5 * groupGap;
            frames.Add(new ColumnFrame(
                BoxOf(x, top, Math.Max(gutter + choiceCount * pitch, (int)slot - 8), height),
                questions,
                gutter,
                0,
                rowHeight,
                choiceCount,
                OmrSectionKinds.BonDapAn));
        }

        return new Plan(
            OmrFormStyles.PhieuThi,
            bubbles,
            frames,
            BoxOf(proctorX, tableTop, madeX + madeWidth - proctorX, codeHeight),
            BoxOf(22, 14, 70, 70),
            choiceCount,
            sections,
            [],
            [],
            fields,
            codes);
    }

    // Lưới số báo danh và mã đề nhỏ, sát mép phải như phiếu thi in sẵn.
    private static CodeGrid SmallCode(string title, int x, int y, int columns, int cell, int label, int head)
    {
        const int bubble = 11;
        var bubbles = new List<CodeBubble>();
        for (var column = 0; column < columns; column++)
        {
            for (var digit = 0; digit < 10; digit++)
            {
                var left = x + label + column * cell + (cell - bubble) / 2;
                var top = y + head + digit * cell + (cell - bubble) / 2;
                bubbles.Add(new CodeBubble(column, digit, BoxOf(left, top, bubble, bubble)));
            }
        }

        return new CodeGrid(
            title,
            BoxOf(x, y, label + columns * cell, head + 10 * cell),
            columns,
            label,
            head,
            cell,
            bubbles);
    }

    // Phiếu kiểu khung: tối đa bốn cột, mỗi cột năm câu, các cột cách nhau một khoảng.
    private static Plan BuildKhung(int questionCount, int choiceCount)
    {
        const int gap = 16;
        const int across = 4;
        const int blockRows = 5;
        const int headerRow = 22;
        const int rowHeight = 28;
        const int gutter = 30;
        const int pad = 10;
        const int bubble = 15;
        var blocks = (int)Math.Ceiling(questionCount / (double)blockRows);
        var perRow = Math.Min(across, Math.Max(1, blocks));
        // Cột rộng vừa cụm ô đáp án, cả hàng canh giữa trang.
        var pitch = 42;
        var blockWidth = gutter + choiceCount * pitch + pad;
        var band = across * blockWidth + (across - 1) * gap;
        if (band > TableWidth)
        {
            var room = TableWidth - (across - 1) * gap - across * (gutter + pad);
            pitch = Math.Max(bubble + 8, room / (across * choiceCount));
            blockWidth = gutter + choiceCount * pitch + pad;
            band = across * blockWidth + (across - 1) * gap;
        }

        var origin = SheetMargin + Math.Max(0, (TableWidth - band) / 2);
        // Khung kê khai cùng bề rộng với hàng cột đáp án, hai mép thẳng nhau.
        var identity = Identity(origin, band);

        var frames = new List<ColumnFrame>();
        var bubbles = new List<BubbleSlot>();
        var top = HeaderBottom + 8;
        const int rowGap = 10;
        var fullHeight = headerRow + blockRows * rowHeight;
        for (var block = 0; block < blocks; block++)
        {
            var count = Math.Min(blockRows, questionCount - block * blockRows);
            var x = origin + block % perRow * (blockWidth + gap);
            var y = top + block / perRow * (fullHeight + rowGap);
            var questions = new int[count];
            var start = block * blockRows + 1;
            for (var line = 0; line < count; line++)
            {
                var number = start + line;
                questions[line] = number;
                for (var choice = 0; choice < choiceCount; choice++)
                {
                    var bubbleLeft = x + gutter + choice * pitch + (pitch - bubble) / 2;
                    var bubbleTop = y + headerRow + line * rowHeight + (rowHeight - bubble) / 2;
                    bubbles.Add(new BubbleSlot(number, choice, BoxOf(bubbleLeft, bubbleTop, bubble, bubble)));
                }
            }

            frames.Add(new ColumnFrame(
                BoxOf(x, y, blockWidth, headerRow + count * rowHeight),
                questions,
                gutter,
                headerRow,
                rowHeight,
                choiceCount,
                OmrSectionKinds.BonDapAn));
        }

        return new Plan(
            OmrFormStyles.Khung,
            bubbles,
            frames,
            identity.Header,
            identity.Qr,
            choiceCount,
            [],
            [],
            [],
            identity.Fields,
            identity.Codes);
    }

    // Đầu phiếu Bộ đo từ mẫu 590×842: hai hộp tách nhau, số báo danh tám cột, mã đề bốn cột.
    private static IdentitySheet MinistryIdentity()
    {
        const double pdfWidth = 589.6063232421875;
        const double pdfHeight = 841.8897705078125;
        int Px(double value) => (int)Math.Round(value * PageWidth / pdfWidth);
        int Py(double value) => (int)Math.Round(value * PageHeight / pdfHeight);
        // Nét chấm vẽ thấp hơn chân chữ 3px, nên đẩy dòng lên để chấm đúng cao độ mẫu.
        int LineTop(double dotY) => Py(dotY) - 17;

        var signLeft = Px(24.7);
        var signTop = Py(78.1);
        var signRight = Px(152.3);
        var signBottom = Py(240.7);
        var signMid = Py(159.4);
        var infoLeft = Px(158.3);
        var infoRight = Px(391.5);
        var fields = new List<LineField>
        {
            new("Kỳ thi:", BoxOf(Px(168), LineTop(49), Px(361) - Px(168), 16)),
            new("Môn thi:", BoxOf(Px(24.5), LineTop(68), Px(248) - Px(24.5), 16)),
            new("Ngày thi:", BoxOf(Px(252), LineTop(68), Px(392) - Px(252), 16)),
            new("Giám thị 1", BoxOf(signLeft, signTop, signRight - signLeft, signMid - signTop)),
            new("Giám thị 2", BoxOf(signLeft, signMid, signRight - signLeft, signBottom - signMid))
        };
        string[] labels =
        [
            "1. Hội đồng thi:",
            "2. Điểm thi:",
            "3. Phòng thi số:",
            "4. Họ và tên thí sinh:",
            "5. Ngày sinh:",
            "6. Chữ ký của thí sinh:"
        ];
        var dotY = new[] { 97d, 125d, 152d, 179d, 206d, 230d };
        var fieldLeft = Px(164);
        var fieldWidth = Px(384) - fieldLeft;
        for (var index = 0; index < labels.Length; index++)
            fields.Add(new LineField(labels[index], BoxOf(fieldLeft, LineTop(dotY[index]), fieldWidth, 16)));

        var codes = new List<CodeGrid>
        {
            MinistryCode("7. Số báo danh", 407.4, 11.3, 8, Px, Py),
            MinistryCode("8. Mã đề thi", 510.2, 11.4, 4, Px, Py)
        };
        var gridBottom = Py(88.0 + 10 * 16.6);
        var codeRight = Px(510.2 + 3 * 11.4 + 11.5);
        return new IdentitySheet(
            BoxOf(signLeft, signTop, codeRight - signLeft, gridBottom - signTop),
            BoxOf(14, 10, 40, 40),
            fields,
            codes);
    }

    // Ô viết số nằm trên lưới. Chữ số 0–9 nằm trong vòng tròn, không có cột nhãn bên trái.
    private static CodeGrid MinistryCode(string title, double originX, double pitch, int columns, Func<double, int> px, Func<double, int> py)
    {
        const double cellW = 11.5;
        const double writeTop = 61.4;
        const double writeH = 16.8;
        const double rowTop = 88.0;
        const double rowH = 16.6;
        const double bubbleW = 8.2;
        const double bubbleH = 7.6;
        var bubbles = new List<CodeBubble>();
        for (var column = 0; column < columns; column++)
        {
            var centerX = originX + cellW / 2 + column * pitch;
            for (var digit = 0; digit < 10; digit++)
            {
                var centerY = rowTop + rowH / 2 + digit * rowH;
                var left = px(centerX - bubbleW / 2);
                var top = py(centerY - bubbleH / 2);
                bubbles.Add(new CodeBubble(column, digit, BoxOf(left, top, Math.Max(1, px(centerX + bubbleW / 2) - left), Math.Max(1, py(centerY + bubbleH / 2) - top))));
            }
        }

        var gridLeft = px(originX);
        var gridTop = py(writeTop);
        var gridRight = px(originX + (columns - 1) * pitch + cellW);
        var gridBottom = py(rowTop + 10 * rowH);
        return new CodeGrid(title, BoxOf(gridLeft, gridTop, gridRight - gridLeft, gridBottom - gridTop), columns, 0, py(writeH), py(rowH), bubbles);
    }

    // Phần đầu giống phiếu thi: dòng kê khai, ô số báo danh và ô mã đề. Rộng bằng hàng cột đáp án.
    private static IdentitySheet Identity(int left, int width)
    {
        var sbdWidth = CodeLabel + 6 * CodeCell;
        var examWidth = CodeLabel + 3 * CodeCell;
        var fieldsWidth = width - QrColumn - sbdWidth - examWidth;
        var sbdX = left + QrColumn + fieldsWidth;
        var examX = sbdX + sbdWidth;
        var gridTop = TableTop + TitleStrip;
        var codes = new List<CodeGrid>
        {
            MakeCodeGrid("7. Số báo danh", sbdX, gridTop, 6),
            MakeCodeGrid("8. Mã đề", examX, gridTop, 3)
        };
        var fieldX = left + QrColumn + 10;
        var fieldW = fieldsWidth - 20;
        string[] labels =
        [
            "1. Hội đồng thi:",
            "2. Mã học sinh:",
            "3. Phòng thi số:",
            "4. Họ và tên:",
            "5. Lớp:",
            "6. Chữ kí của thí sinh:"
        ];
        var step = (GridHeight - 16) / labels.Length;
        var half = width / 2;
        var fields = new List<LineField>
        {
            new("Kỳ thi:", BoxOf(left + (width - 360) / 2, 52, 360, 16)),
            new("Môn thi:", BoxOf(left, 72, half - 12, 16)),
            new("Ngày thi:", BoxOf(left + half, 72, half, 16))
        };
        for (var index = 0; index < labels.Length; index++)
            fields.Add(new LineField(labels[index], BoxOf(fieldX, gridTop + 4 + index * step, fieldW, 16)));

        return new IdentitySheet(
            BoxOf(left, TableTop, width, TableHeight),
            BoxOf(left + 16, TableTop + 18, 78, 78),
            fields,
            codes);
    }

    private static CodeGrid MakeCodeGrid(string title, int x, int y, int columns)
    {
        var bubbles = new List<CodeBubble>(columns * 10);
        for (var digit = 0; digit < 10; digit++)
        {
            for (var column = 0; column < columns; column++)
            {
                var left = x + CodeLabel + column * CodeCell + (CodeCell - CodeBubbleSize) / 2;
                var top = y + CodeHead + digit * CodeCell + (CodeCell - CodeBubbleSize) / 2;
                bubbles.Add(new CodeBubble(column, digit, BoxOf(left, top, CodeBubbleSize, CodeBubbleSize)));
            }
        }

        return new CodeGrid(
            title,
            BoxOf(x, y, CodeLabel + columns * CodeCell, GridHeight),
            columns,
            CodeLabel,
            CodeHead,
            CodeCell,
            bubbles);
    }

    // Khổ và khoảng cách lấy từ phiếu kiểm tra A4 scan 300 dpi, thu về trang vẽ 1200×1697.
    private static Plan BuildKiemTra(int partOne, int partTwo)
    {
        const int headLeft = 86;
        const int headTop = 117;
        const int headWidth = 745;
        const int headHeight = 405;
        const int split = 286;
        const int row1 = 131;
        const int row2 = 131;
        var fields = new List<LineField>
        {
            new("Cán bộ coi thi", BoxOf(headLeft, headTop, split, row1)),
            new("Giám khảo", BoxOf(headLeft, headTop + row1, split, row2)),
            new("Điểm bài kiểm tra", BoxOf(headLeft, headTop + row1 + row2, split, headHeight - row1 - row2)),
            new("TRƯỜNG:", BoxOf(headLeft + split + 16, headTop + 22, headWidth - split - 32, 22)),
            new("1. Kỳ kiểm tra:", BoxOf(headLeft + split + 16, headTop + 70, headWidth - split - 32, 22)),
            new("2. Môn kiểm tra:", BoxOf(headLeft + split + 16, headTop + 118, headWidth - split - 32, 22)),
            new("3. Phòng kiểm tra:", BoxOf(headLeft + split + 16, headTop + 166, 280, 22)),
            new("Lớp:", BoxOf(headLeft + split + 310, headTop + 166, headWidth - split - 326, 22)),
            new("4. Họ và tên thí sinh:", BoxOf(headLeft + split + 16, headTop + 214, headWidth - split - 32, 22)),
            new("5. Ngày sinh:", BoxOf(headLeft + split + 16, headTop + 310, headWidth - split - 32, 22)),
            new("6. Chữ ký của thí sinh:", BoxOf(headLeft + split + 16, headTop + 358, headWidth - split - 32, 22))
        };

        const int cellW = 23;
        const int writeTop = 132;
        const int digitTop = 172;
        const int cellH = 33;
        const int bubble = 16;
        var codes = new List<CodeGrid>
        {
            CheckCode("7. Số báo danh", 868, 6, cellW, writeTop, digitTop, cellH, bubble),
            CheckCode("8. Mã đề thi", 1039, 3, cellW, writeTop, digitTop, cellH, bubble)
        };

        const int colLeft = 125;
        const int colTop = 606;
        const int colW = 210;
        const int colGap = 36;
        const int colHead = 34;
        const int gutter = 42;
        const int answerShift = 10;
        const int bottomPad = 8;
        const int perColumn = 10;
        const int across = 4;
        const int bandGap = 16;
        const int pageBottom = 1620;
        // Hết bốn cột mười câu thì xuống hàng dưới. Hàng nhiều thì hạ chiều cao ô để còn vừa trang A4.
        var columnCount = partOne == 0 ? 0 : (int)Math.Ceiling(partOne / (double)perColumn);
        var bands = columnCount == 0 ? 0 : (int)Math.Ceiling(columnCount / (double)across);
        var truthBlock = partTwo > 0 ? 30 + 48 + 4 * 31 : 0;
        var room = pageBottom - colTop - truthBlock - Math.Max(0, bands - 1) * bandGap - bands * bottomPad;
        var rowH = 26;
        if (bands > 0)
        {
            var fit = (room / bands - colHead) / perColumn;
            if (fit < rowH)
                rowH = Math.Max(20, fit);
        }

        var choiceBubble = Math.Min(18, Math.Max(14, rowH - 8));
        var pitch = (colW - gutter) / 4d;
        var frames = new List<ColumnFrame>();
        var bubbles = new List<BubbleSlot>();
        var boxHeight = colHead + perColumn * rowH + bottomPad;
        for (var column = 0; column < columnCount; column++)
        {
            var count = Math.Min(perColumn, partOne - column * perColumn);
            var questions = new int[count];
            var x = colLeft + column % across * (colW + colGap);
            var y = colTop + column / across * (boxHeight + bandGap);
            for (var row = 0; row < count; row++)
            {
                var number = column * perColumn + row + 1;
                questions[row] = number;
                // Kéo cụm A–D lại gần số câu, chừa lề dưới khung thêm một chút.
                for (var choice = 0; choice < 4; choice++)
                {
                    var left = (int)Math.Round(x + gutter - answerShift + choice * pitch + (pitch - choiceBubble) / 2);
                    var top = y + colHead + row * rowH + (rowH - choiceBubble) / 2;
                    bubbles.Add(new BubbleSlot(number, choice, BoxOf(left, top, choiceBubble, choiceBubble)));
                }
            }

            frames.Add(new ColumnFrame(
                BoxOf(x, y, colW, boxHeight),
                questions,
                gutter,
                colHead,
                rowH,
                4,
                OmrSectionKinds.BonDapAn));
        }

        var titles = new List<SectionTitle>();
        var truths = new List<TruthBlock>();
        if (partOne > 0)
            titles.Add(new SectionTitle(BoxOf(colLeft, colTop - 32, 160, 24), "PHẦN I"));
        if (partTwo > 0)
        {
            var columnsBottom = bands == 0 ? colTop : colTop + bands * boxHeight + (bands - 1) * bandGap;
            var tableTop = columnsBottom + 36;
            const int head = 48;
            const int truthRow = 31;
            var slot = colW / 2;
            var tableWidth = slot * partTwo;
            var letters = new[] { "a,", "b,", "c,", "d," };
            var rows = new List<TruthRow>();
            var number = partOne + 1;
            for (var question = 0; question < partTwo; question++)
            {
                for (var line = 0; line < 4; line++)
                {
                    rows.Add(new TruthRow(number, question == 0 ? letters[line] : ""));
                    var y = tableTop + head + line * truthRow + (truthRow - choiceBubble) / 2;
                    var pairLeft = colLeft + question * slot;
                    bubbles.Add(new BubbleSlot(number, 0, BoxOf(pairLeft + 31, y, choiceBubble, choiceBubble)));
                    bubbles.Add(new BubbleSlot(number, 1, BoxOf(pairLeft + 67, y, choiceBubble, choiceBubble)));
                    number++;
                }
            }

            titles.Add(new SectionTitle(BoxOf(colLeft, tableTop - 30, 160, 24), "PHẦN II"));
            truths.Add(new TruthBlock(
                BoxOf(colLeft, tableTop, tableWidth, head + 4 * truthRow),
                "PHẦN II",
                28,
                head,
                truthRow,
                rows));
        }

        return new Plan(
            OmrFormStyles.KiemTra,
            bubbles,
            frames,
            BoxOf(headLeft, headTop, headWidth, headHeight),
            BoxOf(18, 12, 42, 42),
            4,
            titles,
            truths,
            [],
            fields,
            codes);
    }

    private static CodeGrid CheckCode(string title, int x, int columns, int cellW, int writeTop, int digitTop, int cellH, int bubble)
    {
        var bubbles = new List<CodeBubble>();
        for (var column = 0; column < columns; column++)
        {
            for (var digit = 0; digit < 10; digit++)
            {
                var left = x + column * cellW + (cellW - bubble) / 2;
                var top = digitTop + digit * cellH + (cellH - bubble) / 2;
                bubbles.Add(new CodeBubble(column, digit, BoxOf(left, top, bubble, bubble)));
            }
        }

        return new CodeGrid(title, BoxOf(x, writeTop, columns * cellW, digitTop - writeTop + 10 * cellH), columns, 0, digitTop - writeTop, cellH, bubbles);
    }

    private readonly record struct IdentitySheet(Box Header, Box Qr, IReadOnlyList<LineField> Fields, IReadOnlyList<CodeGrid> Codes);

    private static Plan BuildTuLuan(int partOne, int partTwo, int partThree)
    {
        // Tọa độ tâm ô theo điểm PDF, rồi đổi sang pixel trang 1200×1697.
        int Sx(double value) => (int)Math.Round(value * PageWidth / 550d);
        int Sy(double value) => (int)Math.Round(value * PageHeight / 770d);
        Box Dot(double cx, double cy, double diameter = 8.2)
        {
            var left = Sx(cx - diameter / 2);
            var top = Sy(cy - diameter / 2);
            var right = Sx(cx + diameter / 2);
            var bottom = Sy(cy + diameter / 2);
            return BoxOf(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));
        }

        var bubbles = new List<BubbleSlot>();
        var number = 1;
        var choiceX = new[] { 57.8, 68.8, 79.8, 90.8 };
        var questions = new List<int>();
        for (var row = 0; row < partOne; row++)
        {
            questions.Add(number);
            for (var choice = 0; choice < 4; choice++)
                bubbles.Add(new BubbleSlot(number, choice, Dot(choiceX[choice], 277.8 + row * 11)));
            number++;
        }

        var frames = new List<ColumnFrame>();
        if (partOne > 0)
        {
            frames.Add(new ColumnFrame(
                BoxOf(Sx(40), Sy(248), Sx(102) - Sx(40), Sy(366) - Sy(248)),
                questions,
                Sx(16),
                Sy(28),
                Math.Max(1, Sy(11)),
                4,
                OmrSectionKinds.BonDapAn));
        }

        var truthSlots = new (double Dung, double Sai, double Top)[]
        {
            (133.4, 144.4, 277.8),
            (188.4, 199.4, 277.8),
            (243.4, 254.4, 277.8),
            (133.4, 144.4, 365.8),
            (188.4, 199.4, 365.8),
            (243.4, 254.4, 365.8)
        };
        var truths = new List<TruthBlock>();
        var letters = new[] { "a)", "b)", "c)", "d)" };
        for (var question = 0; question < partTwo; question++)
        {
            var slot = truthSlots[question];
            var rows = new List<TruthRow>();
            for (var line = 0; line < 4; line++)
            {
                rows.Add(new TruthRow(number, letters[line]));
                bubbles.Add(new BubbleSlot(number, 0, Dot(slot.Dung, slot.Top + line * 11)));
                bubbles.Add(new BubbleSlot(number, 1, Dot(slot.Sai, slot.Top + line * 11)));
                number++;
            }

            truths.Add(new TruthBlock(
                BoxOf(Sx(slot.Dung - 16), Sy(slot.Top - 28), Sx(36), Sy(11 * 4 + 24)),
                $"Câu {question + 1}",
                Sx(14),
                Sy(16),
                Math.Max(1, Sy(11)),
                rows));
        }

        var digitColumns = new[]
        {
            new[] { 319.0, 330.0, 341.0, 352.0 },
            new[] { 394.6, 405.6, 416.6, 427.6 },
            new[] { 470.2, 481.2, 492.2, 503.2 }
        };
        var digits = new List<DigitBlock>();
        for (var question = 0; question < partThree; question++)
        {
            var columns = digitColumns[question];
            for (var row = 0; row < OmrDigitGrid.Rows; row++)
            {
                for (var column = 0; column < OmrDigitGrid.Columns; column++)
                {
                    if (!OmrSectionKinds.EssayDigitCell(column, row))
                        continue;
                    bubbles.Add(new BubbleSlot(number, OmrDigitGrid.Index(column, row), Dot(columns[column], 277.8 + row * 11)));
                }
            }

            digits.Add(new DigitBlock(
                BoxOf(Sx(columns[0] - 18), Sy(250), Sx(columns[3] + 8) - Sx(columns[0] - 18), Sy(410) - Sy(250)),
                $"Câu {question + 1}",
                number,
                Sx(16),
                Sy(24),
                Math.Max(1, Sy(11))));
            number++;
        }

        var titles = new List<SectionTitle>();
        if (partOne > 0)
            titles.Add(new SectionTitle(BoxOf(Sx(41), Sy(232), Sx(70), Sy(16)), "PHẦN I"));
        if (partTwo > 0)
            titles.Add(new SectionTitle(BoxOf(Sx(117), Sy(232), Sx(80), Sy(16)), "PHẦN II"));
        if (partThree > 0)
            titles.Add(new SectionTitle(BoxOf(Sx(302), Sy(232), Sx(80), Sy(16)), "PHẦN III"));
        titles.Add(new SectionTitle(
            BoxOf(Sx(116.9), Sy(426.2), Sx(508.8) - Sx(116.9), Sy(701.2) - Sy(426.2)),
            "PHẦN TỰ LUẬN"));

        var fields = new List<LineField>
        {
            new("Kỳ thi:", BoxOf(Sx(103), Sy(50), Sx(240), Sy(14))),
            new("Bài thi:", BoxOf(Sx(103), Sy(64), Sx(130), Sy(14))),
            new("Ngày thi:", BoxOf(Sx(240), Sy(64), Sx(110), Sy(14))),
            new("Cán bộ coi thi 1", BoxOf(Sx(103.1), Sy(89.4), Sx(116.9), Sy(55))),
            new("Cán bộ coi thi 2", BoxOf(Sx(226.9), Sy(89.4), Sx(116.9), Sy(55))),
            new("1. Họ và tên thí sinh:", BoxOf(Sx(112), Sy(158), Sx(326) - Sx(112), Sy(16))),
            new("2. Phòng thi số:", BoxOf(Sx(112), Sy(180), Sx(326) - Sx(112), Sy(16))),
            new("3. Ngày sinh:", BoxOf(Sx(112), Sy(202), Sx(326) - Sx(112), Sy(16)))
        };

        CodeGrid Code(string title, double labelX, double firstX, int columns) 
        {
            var marks = new List<CodeBubble>();
            for (var column = 0; column < columns; column++)
            {
                for (var digit = 0; digit < 10; digit++)
                    marks.Add(new CodeBubble(column, digit, Dot(firstX + column * 11, 105.9 + digit * 11)));
            }

            return new CodeGrid(
                title,
                BoxOf(Sx(firstX - 8), Sy(66), Sx(columns * 11 + 6), Sy(150)),
                columns,
                Sx(labelX),
                Sy(28),
                Math.Max(1, Sy(11)),
                marks);
        }

        var codes = new List<CodeGrid>
        {
            Code("9. Điểm tự luận", 44.8, 57.8, 3),
            Code("7. Số báo danh", 357.6, 370.6, 8),
            Code("8. Mã đề", 460.8, 473.7, 4)
        };

        return new Plan(
            OmrFormStyles.TuLuan,
            bubbles,
            frames,
            // Khung ngoài ôm hết trang phiếu.
            BoxOf(Sx(27.5), Sy(27.5), Sx(495), Sy(715)),
            BoxOf(Sx(32), Sy(30), 36, 36),
            4,
            titles,
            truths,
            digits,
            fields,
            codes);
    }

    private static Box BoxOf(int left, int top, int width, int height) =>
        new(left / (double)PageWidth, top / (double)PageHeight, width / (double)PageWidth, height / (double)PageHeight);
}
