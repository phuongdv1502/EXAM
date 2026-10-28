namespace AIQuizHSG.Application.Common;

// Giờ của máy chủ. Bài thi dùng giờ này, không lấy giờ từ trình duyệt.
public interface IClock
{
    DateTime UtcNow { get; }
}

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
