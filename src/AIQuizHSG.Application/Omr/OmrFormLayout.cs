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
        config.Sections.Count > 0
            ? MeasureParts(config.Sections)
            : Measure(config.QuestionCount, config.ChoiceCount, config.Style);

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
                titles.Add(new SectionTitle(BoxOf(left, y, width, 18), "PHẦN I"));
                y += 22;
                y = AddChoiceColumns(columns, bubbles, ref question, section.QuestionCount, left, y, width);
            }
            else if (section.Kind == OmrSectionKinds.DungSai)
            {
                titles.Add(new SectionTitle(BoxOf(left, y, width, 18), "PHẦN II"));
                y += 22;
                y = AddTruth(truths, bubbles, ref question, section.QuestionCount, left, y, width);
            }
            else if (section.Kind == OmrSectionKinds.DienSo)
            {
                titles.Add(new SectionTitle(BoxOf(left, y, width, 18), "PHẦN III"));
                y += 22;
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

    private static int AddChoiceColumns(List<ColumnFrame> frames, List<BubbleSlot> bubbles, ref int question, int questionCount, int left, int top, int width)
    {
        const int gap = 14;
        const int headerRow = 26;
        const int rowHeight = 30;
        var columns = Math.Clamp((int)Math.Ceiling(questionCount / 10d), 1, 4);
        var rows = (int)Math.Ceiling(questionCount / (double)columns);
        var columnWidth = (width - gap * 3) / 4;
        var height = headerRow + rows * rowHeight;
        var gutter = 42;
        var cell = (columnWidth - gutter) / 4d;
        var bubble = 16;
        var start = question;

        for (var column = 0; column < columns; column++)
        {
            var questions = new int[rows];
            var x = left + column * (columnWidth + gap);
            for (var row = 0; row < rows; row++)
            {
                var number = start + column * rows + row;
                if (number >= start + questionCount)
                    continue;
                questions[row] = number;
                for (var choice = 0; choice < 4; choice++)
                {
                    var cellLeft = x + gutter + choice * cell;
                    var bubbleLeft = (int)Math.Round(cellLeft + (cell - bubble) / 2);
                    var bubbleTop = top + headerRow + row * rowHeight + (rowHeight - bubble) / 2;
                    bubbles.Add(new BubbleSlot(number, choice, BoxOf(bubbleLeft, bubbleTop, bubble, bubble)));
                }
            }

            if (questions.Any(item => item > 0))
                frames.Add(new ColumnFrame(BoxOf(x, top, columnWidth, height), questions, gutter, headerRow, rowHeight, 4, OmrSectionKinds.BonDapAn));
        }

        question += questionCount;
        return top + height;
    }

    // Phần II: bốn ô, mỗi ô hai câu đúng/sai đặt cạnh nhau như phiếu Bộ.
    private static int AddTruth(List<TruthBlock> blocks, List<BubbleSlot> bubbles, ref int question, int questionCount, int left, int top, int width)
    {
        const int gap = 10;
        const int across = 4;
        const int header = 36;
        const int rowHeight = 24;
        const int bubble = 14;
        const int pitch = 30;
        const int label = 22;
        var pairWidth = (width - gap * (across - 1)) / across;
        var half = pairWidth / 2;
        var blockHeight = header + 4 * rowHeight;
        var perRow = across * 2;
        var rows = (int)Math.Ceiling(questionCount / (double)perRow);

        for (var index = 0; index < questionCount; index++)
        {
            var slot = index / 2;
            var side = index % 2;
            var row = slot / across;
            var column = slot % across;
            var sideWidth = side == 0 ? half : pairWidth - half;
            var x = left + column * (pairWidth + gap) + (side == 0 ? 0 : half);
            var y = top + row * (blockHeight + gap);
            var inset = Math.Max(6, (sideWidth - (label + pitch * 2)) / 2);
            var lines = new List<TruthRow>();
            for (var item = 0; item < 4; item++)
            {
                lines.Add(new TruthRow(question, ((char)('a' + item)).ToString()));
                for (var choice = 0; choice < 2; choice++)
                {
                    var bubbleLeft = x + inset + label + choice * pitch;
                    var bubbleTop = y + header + item * rowHeight + (rowHeight - bubble) / 2;
                    bubbles.Add(new BubbleSlot(question, choice, BoxOf(bubbleLeft, bubbleTop, bubble, bubble)));
                }

                question++;
            }

            blocks.Add(new TruthBlock(BoxOf(x, y, sideWidth, blockHeight), $"Câu {index + 1}", inset, header, rowHeight, lines));
        }

        return top + rows * blockHeight + Math.Max(0, rows - 1) * gap;
    }

    private static int AddDigits(List<DigitBlock> blocks, List<BubbleSlot> bubbles, ref int question, int questionCount, int left, int top, int width)
    {
        const int gap = 8;
        const int header = 24;
        const int perRow = 6;
        var blockWidth = (width - gap * (perRow - 1)) / perRow;
        var label = 26;
        var cell = Math.Clamp((blockWidth - label - 8) / OmrDigitGrid.Columns, 18, 34);
        const int bubble = 14;
        var blockHeight = header + OmrDigitGrid.Rows * cell;
        var rows = (int)Math.Ceiling(questionCount / (double)perRow);

        for (var index = 0; index < questionCount; index++)
        {
            var row = index / perRow;
            var column = index % perRow;
            var x = left + column * (blockWidth + gap);
            var y = top + row * (blockHeight + gap);
            var gridLeft = x + label + (blockWidth - label - OmrDigitGrid.Columns * cell) / 2;
            for (var digitRow = 0; digitRow < OmrDigitGrid.Rows; digitRow++)
            {
                for (var digitColumn = 0; digitColumn < OmrDigitGrid.Columns; digitColumn++)
                {
                    var bubbleLeft = (int)Math.Round(gridLeft + digitColumn * cell + (cell - bubble) / 2d);
                    var bubbleTop = y + header + digitRow * cell + (cell - bubble) / 2;
                    bubbles.Add(new BubbleSlot(question, OmrDigitGrid.Index(digitColumn, digitRow), BoxOf(bubbleLeft, bubbleTop, bubble, bubble)));
                }
            }

            blocks.Add(new DigitBlock(BoxOf(x, y, blockWidth, blockHeight), $"Câu {index + 1}", question, label, header, cell));
            question++;
        }

        return top + rows * blockHeight + Math.Max(0, rows - 1) * gap;
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

    // Đầu phiếu trắc nghiệm mẫu Bộ: giám thị, sáu dòng kê khai, số báo danh và mã đề.
    private static IdentitySheet MinistryIdentity()
    {
        const int tableTop = 100;
        const int cell = 15;
        const int label = 16;
        const int head = 14;
        var height = head + 10 * cell;
        var madeWidth = label + 3 * cell;
        var sbdWidth = label + 6 * cell;
        var madeX = PageWidth - 40 - madeWidth;
        var sbdX = madeX - 10 - sbdWidth;
        const int proctorX = 24;
        const int proctorW = 150;
        var fieldX = proctorX + proctorW;
        var fieldW = sbdX - 12 - fieldX;
        var half = height / 2;
        var fields = new List<LineField>
        {
            new("Kỳ thi:", BoxOf(360, 52, 440, 16)),
            new("Môn thi:", BoxOf(96, 76, 460, 16)),
            new("Ngày thi:", BoxOf(640, 76, 250, 16)),
            new("Giám thị 1", BoxOf(proctorX, tableTop, proctorW, half)),
            new("Giám thị 2", BoxOf(proctorX, tableTop + half, proctorW, height - half))
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
        var step = (height - 14) / labels.Length;
        for (var index = 0; index < labels.Length; index++)
            fields.Add(new LineField(labels[index], BoxOf(fieldX + 10, tableTop + 6 + index * step, fieldW - 20, 16)));

        var codes = new List<CodeGrid>
        {
            SmallCode("7. Số báo danh", sbdX, tableTop, 6, cell, label, head),
            SmallCode("8. Mã đề thi", madeX, tableTop, 3, cell, label, head)
        };
        return new IdentitySheet(
            BoxOf(proctorX, tableTop, madeX + madeWidth - proctorX, height),
            BoxOf(16, 12, 58, 58),
            fields,
            codes);
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

    private readonly record struct IdentitySheet(Box Header, Box Qr, IReadOnlyList<LineField> Fields, IReadOnlyList<CodeGrid> Codes);

    private static Box BoxOf(int left, int top, int width, int height) =>
        new(left / (double)PageWidth, top / (double)PageHeight, width / (double)PageWidth, height / (double)PageHeight);
}
