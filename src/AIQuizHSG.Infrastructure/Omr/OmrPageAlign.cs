using AIQuizHSG.Application.Omr;
using SkiaSharp;

namespace AIQuizHSG.Infrastructure.Omr;

// Ảnh chụp còn bàn xung quanh. Kéo tờ phiếu về đúng trang A4 rồi mới đọc ô tô.
internal static class OmrPageAlign
{
    public static SKBitmap Straighten(SKBitmap source)
    {
        var corners = FindCorners(source);
        if (corners is null)
            return source;
        return Warp(source, corners) ?? source;
    }

    // Scan phẳng không có mép giấy. Dùng ô đen cạnh các cột để kéo phiếu về đúng mẫu.
    public static SKBitmap AlignForm(SKBitmap source, OmrTemplateConfig config)
    {
        var columns = OmrFormLayout.For(config).Columns;
        if (columns.Count < 2)
            return source;
        var expected = columns.Select(column =>
        {
            var frame = OmrFormLayout.Pixels(column.Area);
            return new Point(frame.Left + frame.Width + 10, frame.Top + frame.Height / 2d - 5);
        }).ToList();
        var blobs = SolidMarks(source);
        if (blobs.Count < 2)
            return source;
        var row = MarkRow(blobs, expected.Count, source.Width);
        // Hai ô tô tình cờ cách đều một bước cột không đủ làm mốc.
        if (row.Count < Math.Min(3, expected.Count))
            return source;

        var used = Math.Min(row.Count, expected.Count);
        var observedPitch = (row[used - 1].X - row[0].X) / (used - 1);
        var templatePitch = (expected[used - 1].X - expected[0].X) / (used - 1);
        if (templatePitch < 1 || observedPitch < 1)
            return source;
        var scaleX = observedPitch / templatePitch;
        if (scaleX < 0.6 || scaleX > 4)
            return source;
        // Mốc cột đầu có thể bị mờ. Chọn cột khởi đầu sao cho trang nằm giữa ảnh nhất.
        var first = Enumerable.Range(0, expected.Count - used + 1)
            .OrderBy(index => Math.Abs(row[0].X - expected[index].X * scaleX + OmrFormLayout.PageWidth * scaleX / 2 - source.Width / 2d))
            .First();
        expected = expected.Skip(first).ToList();
        var originX = row[0].X - expected[0].X * scaleX;
        var markY = row.Take(used).Average(item => item.Y);
        var top = blobs
            .Where(item => Math.Abs(item.X - (originX + (OmrFormLayout.PageWidth - 36) * scaleX)) < 40 * scaleX && item.Y < markY - 80)
            .OrderBy(item => item.Y)
            .FirstOrDefault();
        double scaleY;
        double originY;
        if (top != default && expected[0].Y - 28 > 1)
        {
            scaleY = (markY - top.Y) / (expected[0].Y - 28);
            originY = top.Y - 28 * scaleY;
        }
        else
        {
            scaleY = scaleX;
            originY = markY - expected[0].Y * scaleY;
        }

        if (scaleY < 0.6 || scaleY > 4)
            return source;
        if (Math.Abs(source.Width - OmrFormLayout.PageWidth) < 2
            && Math.Abs(source.Height - OmrFormLayout.PageHeight) < 2
            && Math.Abs(scaleX - 1) < 0.04
            && Math.Abs(scaleY - 1) < 0.04
            && Math.Abs(originX) < 24
            && Math.Abs(originY) < 24)
            return source;

        Point[] corners =
        [
            new(originX, originY),
            new(originX + OmrFormLayout.PageWidth * scaleX, originY),
            new(originX + OmrFormLayout.PageWidth * scaleX, originY + OmrFormLayout.PageHeight * scaleY),
            new(originX, originY + OmrFormLayout.PageHeight * scaleY)
        ];
        return Warp(source, corners) ?? source;
    }

    private static List<Point> SolidMarks(SKBitmap source)
    {
        var scale = Math.Min(1d, 720d / Math.Max(source.Width, source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        using var small = source.Resize(new SKImageInfo(width, height), SKSamplingOptions.Default);
        if (small is null)
            return [];

        // Ô mốc in cỡ 12 điểm trên trang 1200. Cửa sổ dò nhỏ hơn ô mốc một chút theo cỡ ảnh.
        var window = Math.Max(3, (int)Math.Round(source.Width * 9d / OmrFormLayout.PageWidth * scale));
        var step = Math.Max(1, window / 2);
        var marks = new List<Point>();
        for (var y = 0; y <= height - window; y += step)
        {
            for (var x = 0; x <= width - window; x += step)
            {
                var dark = 0;
                var total = window * window;
                for (var dy = 0; dy < window; dy++)
                {
                    for (var dx = 0; dx < window; dx++)
                    {
                        var pixel = small.GetPixel(x + dx, y + dy);
                        if (pixel.Red * 0.299 + pixel.Green * 0.587 + pixel.Blue * 0.114 < 80)
                            dark++;
                    }
                }

                if (dark < total * 0.72)
                    continue;
                var center = new Point((x + window / 2d) / scale, (y + window / 2d) / scale);
                if (marks.Any(item => Distance(item, center) < 22))
                    continue;
                marks.Add(center);
            }
        }

        return marks;
    }

    // Hàng ô đen cách đều đúng một bước cột. Ô học sinh tô nằm lẫn trong hàng thì bỏ qua.
    private static List<Point> MarkRow(List<Point> blobs, int wanted, int imageWidth)
    {
        var best = new List<Point>();
        var bestError = double.MaxValue;
        foreach (var start in blobs)
        {
            foreach (var next in blobs)
            {
                var pitch = next.X - start.X;
                if (pitch < imageWidth * 0.12 || pitch > imageWidth * 0.3 || Math.Abs(next.Y - start.Y) > pitch * 0.04)
                    continue;
                var tolerance = pitch * 0.04;
                var row = new List<Point> { start };
                var error = 0d;
                for (var step = 1; step < wanted; step++)
                {
                    var wantX = start.X + step * pitch;
                    var hit = blobs
                        .Where(item => Math.Abs(item.X - wantX) <= tolerance && Math.Abs(item.Y - start.Y) <= tolerance)
                        .OrderBy(item => Math.Abs(item.X - wantX) + Math.Abs(item.Y - start.Y))
                        .FirstOrDefault();
                    if (hit == default)
                        break;
                    error += Math.Abs(hit.X - wantX) + Math.Abs(hit.Y - start.Y);
                    row.Add(hit);
                }

                if (row.Count < 2)
                    continue;
                if (row.Count < best.Count || (row.Count == best.Count && error >= bestError))
                    continue;
                best = row;
                bestError = error;
            }
        }

        return best;
    }

    private readonly record struct Point(double X, double Y);

    private static Point[]? FindCorners(SKBitmap source)
    {
        var scale = Math.Min(1d, 480d / Math.Max(source.Width, source.Height));
        var width = Math.Max(1, (int)Math.Round(source.Width * scale));
        var height = Math.Max(1, (int)Math.Round(source.Height * scale));
        using var small = source.Resize(new SKImageInfo(width, height), SKSamplingOptions.Default);
        if (small is null)
            return null;

        var gray = new byte[width * height];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var pixel = small.GetPixel(x, y);
                gray[y * width + x] = (byte)(pixel.Red * 0.299 + pixel.Green * 0.587 + pixel.Blue * 0.114);
            }
        }

        var soft = BoxBlur(gray, width, height);
        // Ngưỡng theo vùng giấy trắng. Otsu thường dính cả mặt bàn sáng vào tờ phiếu.
        Point[]? corners = null;
        var bestScore = double.PositiveInfinity;
        foreach (var threshold in PaperThresholds(soft))
        {
            var paper = GrowPaper(soft, width, height, threshold, Math.Max(1, threshold - 36));
            var blob = LargestBlob(paper, width, height);
            if (blob is null)
                continue;
            var cover = blob.Value.Area / (double)(width * height);
            if (cover < 0.28 || cover > 0.92)
                continue;
            Point[][] options =
            [
                EdgeQuad(paper, width, height) ?? [],
                [blob.Value.TopLeft, blob.Value.TopRight, blob.Value.BottomRight, blob.Value.BottomLeft]
            ];
            foreach (var next in options)
            {
                if (next.Length != 4 || !QuadFits(next))
                    continue;
                var score = QuadScore(next);
                if (score >= bestScore)
                    continue;
                bestScore = score;
                corners = next;
            }
        }

        if (corners is null)
            return null;

        var back = 1d / scale;
        return corners.Select(point => new Point(point.X * back, point.Y * back)).ToArray();
    }

    // Giao bốn cạnh tờ giấy, để một góc bị bóng không kéo cả mép phiếu.
    private static Point[]? EdgeQuad(byte[] paper, int width, int height)
    {
        var mask = LargestMask(paper, width, height);
        if (mask is null)
            return null;

        var top = new List<Point>();
        var bottom = new List<Point>();
        for (var x = 0; x < width; x++)
        {
            var min = -1;
            var max = -1;
            for (var y = 0; y < height; y++)
            {
                if (mask[y * width + x] == 0)
                    continue;
                if (min < 0)
                    min = y;
                max = y;
            }

            if (min < 2 || max < 0)
                continue;
            top.Add(new Point(x, min));
            if (max < height - 2)
                bottom.Add(new Point(x, max));
        }

        var left = new List<Point>();
        var right = new List<Point>();
        for (var y = 0; y < height; y++)
        {
            var min = -1;
            var max = -1;
            for (var x = 0; x < width; x++)
            {
                if (mask[y * width + x] == 0)
                    continue;
                if (min < 0)
                    min = x;
                max = x;
            }

            if (min < 0)
                continue;
            if (min > 1)
                left.Add(new Point(min, y));
            if (max < width - 2)
                right.Add(new Point(max, y));
        }

        if (!TryFit(top, false, out var topSlope, out var topIntercept)
            || !TryFit(bottom.Count >= 12 ? bottom : top.Select(point => new Point(point.X, height - 2)).ToList(), false, out var bottomSlope, out var bottomIntercept)
            || !TryFit(left, true, out var leftSlope, out var leftIntercept)
            || !TryFit(right, true, out var rightSlope, out var rightIntercept))
            return null;

        var topLeft = Intersect(topSlope, topIntercept, leftSlope, leftIntercept);
        var topRight = Intersect(topSlope, topIntercept, rightSlope, rightIntercept);
        var bottomRight = Intersect(bottomSlope, bottomIntercept, rightSlope, rightIntercept);
        var bottomLeft = Intersect(bottomSlope, bottomIntercept, leftSlope, leftIntercept);
        if (topLeft is null || topRight is null || bottomRight is null || bottomLeft is null)
            return null;
        return [topLeft.Value, topRight.Value, bottomRight.Value, bottomLeft.Value];
    }

    private static byte[]? LargestMask(byte[] paper, int width, int height)
    {
        var seen = new byte[paper.Length];
        var stack = new Stack<int>();
        var best = 0;
        byte[]? mask = null;
        for (var start = 0; start < paper.Length; start++)
        {
            if (paper[start] == 0 || seen[start] != 0)
                continue;
            var current = new byte[paper.Length];
            seen[start] = 1;
            current[start] = 1;
            stack.Push(start);
            var area = 0;
            while (stack.Count > 0)
            {
                var index = stack.Pop();
                area++;
                var x = index % width;
                var y = (index - x) / width;
                void Consider(int nextX, int nextY)
                {
                    if (nextX < 0 || nextY < 0 || nextX >= width || nextY >= height)
                        return;
                    var next = nextY * width + nextX;
                    if (seen[next] != 0 || paper[next] == 0)
                        return;
                    seen[next] = 1;
                    current[next] = 1;
                    stack.Push(next);
                }

                Consider(x + 1, y);
                Consider(x - 1, y);
                Consider(x, y + 1);
                Consider(x, y - 1);
            }

            if (area <= best)
                continue;
            best = area;
            mask = current;
        }

        return mask;
    }

    // vertical: x = slope * y + intercept. Ngang: y = slope * x + intercept.
    private static bool TryFit(List<Point> samples, bool vertical, out double slope, out double intercept)
    {
        slope = 0;
        intercept = 0;
        if (samples.Count < 12)
            return false;
        var independent = samples.Select(point => vertical ? point.Y : point.X).ToList();
        var lo = independent.Min();
        var hi = independent.Max();
        if (hi - lo < 8)
            return false;
        var bins = new List<Point>();
        const int count = 8;
        for (var bin = 0; bin < count; bin++)
        {
            var from = lo + (hi - lo) * bin / count;
            var to = lo + (hi - lo) * (bin + 1) / count;
            var values = new List<double>();
            for (var index = 0; index < samples.Count; index++)
            {
                if (independent[index] < from || independent[index] >= to && bin < count - 1)
                    continue;
                values.Add(vertical ? samples[index].X : samples[index].Y);
            }

            if (values.Count < 3)
                continue;
            values.Sort();
            var mid = values[values.Count / 2];
            var at = (from + to) / 2;
            bins.Add(vertical ? new Point(mid, at) : new Point(at, mid));
        }

        if (bins.Count < 4)
            return false;
        double sumX = 0, sumY = 0, sumXX = 0, sumXY = 0;
        foreach (var point in bins)
        {
            var x = vertical ? point.Y : point.X;
            var y = vertical ? point.X : point.Y;
            sumX += x;
            sumY += y;
            sumXX += x * x;
            sumXY += x * y;
        }

        var denominator = bins.Count * sumXX - sumX * sumX;
        if (Math.Abs(denominator) < 1e-6)
            return false;
        slope = (bins.Count * sumXY - sumX * sumY) / denominator;
        intercept = (sumY - slope * sumX) / bins.Count;
        return true;
    }

    private static Point? Intersect(double horizontalSlope, double horizontalIntercept, double verticalSlope, double verticalIntercept)
    {
        var denominator = 1 - verticalSlope * horizontalSlope;
        if (Math.Abs(denominator) < 1e-6)
            return null;
        var x = (verticalSlope * horizontalIntercept + verticalIntercept) / denominator;
        var y = horizontalSlope * x + horizontalIntercept;
        return new Point(x, y);
    }

    private static SKBitmap? Warp(SKBitmap source, Point[] corners)
    {
        const int width = 1200;
        const int height = 1697;
        Point[] page =
        [
            new(0, 0),
            new(width - 1, 0),
            new(width - 1, height - 1),
            new(0, height - 1)
        ];
        var homography = Homography(page, corners);
        if (homography is null)
            return null;

        var toSource = new SKMatrix(
            (float)homography[0], (float)homography[1], (float)homography[2],
            (float)homography[3], (float)homography[4], (float)homography[5],
            (float)homography[6], (float)homography[7], 1);
        if (!toSource.TryInvert(out var toPage))
            return null;

        var pageImage = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Premul);
        using var canvas = new SKCanvas(pageImage);
        canvas.Clear(SKColors.White);
        canvas.SetMatrix(toPage);
        canvas.DrawBitmap(source, 0, 0);
        return pageImage;
    }

    private static double[]? Homography(Point[] from, Point[] to)
    {
        var rows = Enumerable.Range(0, 8).Select(_ => new double[9]).ToArray();
        for (var index = 0; index < 4; index++)
        {
            var x = from[index].X;
            var y = from[index].Y;
            var u = to[index].X;
            var v = to[index].Y;
            rows[index * 2] = [x, y, 1, 0, 0, 0, -u * x, -u * y, u];
            rows[index * 2 + 1] = [0, 0, 0, x, y, 1, -v * x, -v * y, v];
        }

        for (var column = 0; column < 8; column++)
        {
            var pivot = column;
            for (var row = column + 1; row < 8; row++)
            {
                if (Math.Abs(rows[row][column]) > Math.Abs(rows[pivot][column]))
                    pivot = row;
            }

            if (Math.Abs(rows[pivot][column]) < 1e-8)
                return null;
            if (pivot != column)
                (rows[column], rows[pivot]) = (rows[pivot], rows[column]);
            var divisor = rows[column][column];
            for (var columnIndex = column; columnIndex < 9; columnIndex++)
                rows[column][columnIndex] /= divisor;
            for (var row = 0; row < 8; row++)
            {
                if (row == column)
                    continue;
                var factor = rows[row][column];
                for (var columnIndex = column; columnIndex < 9; columnIndex++)
                    rows[row][columnIndex] -= factor * rows[column][columnIndex];
            }
        }

        return rows.Select(row => row[8]).ToArray();
    }

    private static byte[] BoxBlur(byte[] source, int width, int height)
    {
        var blurred = new byte[source.Length];
        for (var y = 0; y < height; y++)
        {
            for (var x = 0; x < width; x++)
            {
                var sum = 0;
                var count = 0;
                for (var offsetY = -1; offsetY <= 1; offsetY++)
                {
                    var nextY = y + offsetY;
                    if (nextY < 0 || nextY >= height)
                        continue;
                    for (var offsetX = -1; offsetX <= 1; offsetX++)
                    {
                        var nextX = x + offsetX;
                        if (nextX < 0 || nextX >= width)
                            continue;
                        sum += source[nextY * width + nextX];
                        count++;
                    }
                }

                blurred[y * width + x] = (byte)(sum / count);
            }
        }

        return blurred;
    }

    private static int[] PaperThresholds(byte[] gray)
    {
        var histogram = new int[256];
        foreach (var value in gray)
            histogram[value]++;
        var mode = 0;
        var modeCount = 0;
        for (var tone = 140; tone < 256; tone++)
        {
            if (histogram[tone] <= modeCount)
                continue;
            modeCount = histogram[tone];
            mode = tone;
        }

        var brighter = 0;
        var paperLevel = mode;
        for (var tone = 255; tone >= 0; tone--)
        {
            brighter += histogram[tone];
            if (brighter < gray.Length * 0.38)
                continue;
            paperLevel = tone;
            break;
        }

        return
        [
            Math.Clamp(paperLevel - 6, 1, 254),
            Math.Clamp(paperLevel - 22, 1, 254),
            Otsu(gray)
        ];
    }

    private static int Otsu(byte[] gray)
    {
        var histogram = new int[256];
        foreach (var value in gray)
            histogram[value]++;
        long total = 0;
        for (var tone = 0; tone < 256; tone++)
            total += tone * histogram[tone];
        long backgroundSum = 0;
        var backgroundWeight = 0;
        double best = 0;
        var threshold = 128;
        for (var tone = 0; tone < 256; tone++)
        {
            backgroundWeight += histogram[tone];
            if (backgroundWeight == 0)
                continue;
            var foregroundWeight = gray.Length - backgroundWeight;
            if (foregroundWeight == 0)
                break;
            backgroundSum += tone * histogram[tone];
            var difference = backgroundSum / (double)backgroundWeight - (total - backgroundSum) / (double)foregroundWeight;
            var between = backgroundWeight * foregroundWeight * difference * difference;
            if (between > best)
            {
                best = between;
                threshold = tone;
            }
        }

        return threshold;
    }

    // Giữ phần giấy sáng, rồi lan sang vùng bóng còn dính vào tờ, không lấy bàn tối.
    private static byte[] GrowPaper(byte[] gray, int width, int height, int strong, int weak)
    {
        var paper = new byte[gray.Length];
        var stack = new Stack<int>();
        void Push(int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height)
                return;
            var index = y * width + x;
            if (paper[index] != 0 || gray[index] < weak)
                return;
            paper[index] = 1;
            stack.Push(index);
        }

        for (var index = 0; index < gray.Length; index++)
        {
            if (gray[index] < strong)
                continue;
            var x = index % width;
            var y = (index - x) / width;
            Push(x, y);
        }

        while (stack.Count > 0)
        {
            var index = stack.Pop();
            var x = index % width;
            var y = (index - x) / width;
            Push(x + 1, y);
            Push(x - 1, y);
            Push(x, y + 1);
            Push(x, y - 1);
        }

        return paper;
    }

    private readonly record struct Blob(int Area, Point TopLeft, Point TopRight, Point BottomRight, Point BottomLeft);

    private static Blob? LargestBlob(byte[] paper, int width, int height)
    {
        var seen = new byte[paper.Length];
        var stack = new Stack<int>();
        Blob? best = null;
        for (var start = 0; start < paper.Length; start++)
        {
            if (paper[start] == 0 || seen[start] != 0)
                continue;
            seen[start] = 1;
            stack.Push(start);
            var area = 0;
            var minSum = double.PositiveInfinity;
            var maxSum = double.NegativeInfinity;
            var minDiff = double.PositiveInfinity;
            var maxDiff = double.NegativeInfinity;
            var topLeft = new Point(0, 0);
            var topRight = new Point(0, 0);
            var bottomRight = new Point(0, 0);
            var bottomLeft = new Point(0, 0);
            while (stack.Count > 0)
            {
                var index = stack.Pop();
                var x = index % width;
                var y = (index - x) / width;
                area++;
                var sum = x + y;
                var diff = x - y;
                if (sum < minSum)
                {
                    minSum = sum;
                    topLeft = new Point(x, y);
                }

                if (sum > maxSum)
                {
                    maxSum = sum;
                    bottomRight = new Point(x, y);
                }

                if (diff > maxDiff)
                {
                    maxDiff = diff;
                    topRight = new Point(x, y);
                }

                if (diff < minDiff)
                {
                    minDiff = diff;
                    bottomLeft = new Point(x, y);
                }

                void Consider(int nextX, int nextY)
                {
                    if (nextX < 0 || nextY < 0 || nextX >= width || nextY >= height)
                        return;
                    var next = nextY * width + nextX;
                    if (seen[next] != 0 || paper[next] == 0)
                        return;
                    seen[next] = 1;
                    stack.Push(next);
                }

                Consider(x + 1, y);
                Consider(x - 1, y);
                Consider(x, y + 1);
                Consider(x, y - 1);
            }

            if (best is null || area > best.Value.Area)
                best = new Blob(area, topLeft, topRight, bottomRight, bottomLeft);
        }

        return best;
    }

    private static double QuadScore(Point[] corners)
    {
        var top = Distance(corners[0], corners[1]);
        var right = Distance(corners[1], corners[2]);
        var bottom = Distance(corners[2], corners[3]);
        var left = Distance(corners[3], corners[0]);
        var wide = Math.Max(1, (top + bottom) / 2);
        var tall = (left + right) / 2;
        var slope = Math.Abs(corners[1].Y - corners[0].Y) / Math.Max(1, Math.Abs(corners[1].X - corners[0].X));
        var aspect = Math.Abs(tall / wide - 1.414);
        var uneven = Math.Abs(top - bottom) / wide + Math.Abs(left - right) / Math.Max(1, tall);
        return slope * 5 + aspect + uneven;
    }

    private static bool QuadFits(Point[] corners)
    {
        for (var left = 0; left < 4; left++)
        {
            for (var right = left + 1; right < 4; right++)
            {
                if (Distance(corners[left], corners[right]) < 6)
                    return false;
            }
        }

        var sign = 0;
        for (var index = 0; index < 4; index++)
        {
            var cross = Cross(corners[index], corners[(index + 1) % 4], corners[(index + 2) % 4]);
            if (Math.Abs(cross) < 1)
                return false;
            var next = Math.Sign(cross);
            if (sign != 0 && next != sign)
                return false;
            sign = next;
        }

        var sides = new double[4];
        for (var index = 0; index < 4; index++)
            sides[index] = Distance(corners[index], corners[(index + 1) % 4]);
        var longest = sides.Max();
        var shortest = sides.Min();
        if (shortest < 1 || longest / shortest > 3.4)
            return false;
        for (var index = 0; index < 4; index++)
        {
            var degrees = Angle(corners[(index + 3) % 4], corners[index], corners[(index + 1) % 4]);
            if (degrees < 40 || degrees > 145)
                return false;
        }

        return true;
    }

    private static double Distance(Point a, Point b) =>
        Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    private static double Cross(Point a, Point b, Point c) =>
        (b.X - a.X) * (c.Y - b.Y) - (b.Y - a.Y) * (c.X - b.X);

    private static double Angle(Point a, Point b, Point c)
    {
        var leftX = a.X - b.X;
        var leftY = a.Y - b.Y;
        var rightX = c.X - b.X;
        var rightY = c.Y - b.Y;
        var length = Math.Sqrt(leftX * leftX + leftY * leftY) * Math.Sqrt(rightX * rightX + rightY * rightY);
        if (length < 1)
            return 0;
        var cosine = Math.Clamp((leftX * rightX + leftY * rightY) / length, -1, 1);
        return Math.Acos(cosine) * 180 / Math.PI;
    }
}
