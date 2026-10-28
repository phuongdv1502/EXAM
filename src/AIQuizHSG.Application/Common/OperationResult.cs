namespace AIQuizHSG.Application.Common;

public enum OperationStatus
{
    Ok,
    Invalid,
    NotFound,
    Forbidden
}

// Kết quả nghiệp vụ trước khi API đổi thành 200, 400 hoặc 404.
public sealed class OperationResult<T>
{
    public OperationStatus Status { get; private init; }
    public string? Message { get; private init; }
    public T? Value { get; private init; }

    public static OperationResult<T> Ok(T value) => new() { Status = OperationStatus.Ok, Value = value };

    public static OperationResult<T> Invalid(string message) => new() { Status = OperationStatus.Invalid, Message = message };

    public static OperationResult<T> NotFound(string message) => new() { Status = OperationStatus.NotFound, Message = message };

    public static OperationResult<T> Forbidden(string message) => new() { Status = OperationStatus.Forbidden, Message = message };
}

// Người đang gọi API, lấy từ JWT, kèm địa chỉ IP để ghi nhật ký.
public sealed record Actor(long UserId, string? IpAddress);
