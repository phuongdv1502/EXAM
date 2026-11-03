namespace AIQuizHSG.Application.Omr;

// Nhận diện ảnh nằm sau interface, controller không gọi thuật toán trực tiếp.
public interface IOmrRecognitionService
{
    OmrRecognitionResult Recognize(ReadOnlyMemory<byte> image, OmrTemplateConfig config);

    string? ReadQr(ReadOnlyMemory<byte> image);
}

// Tách PDF nhiều trang thành từng ảnh phiếu. Ảnh đơn trả về một trang.
public interface IOmrPageSplitter
{
    IReadOnlyList<OmrPageImage> Split(string contentType, ReadOnlyMemory<byte> content);
}

// Ảnh lưu ngoài thư mục web. MySQL chỉ giữ đường dẫn tương đối.
public interface IOmrFileStore
{
    Task<OmrStoredFile> SaveAsync(long batchId, string fileName, ReadOnlyMemory<byte> content, CancellationToken ct);
    Task<byte[]?> ReadAsync(string relativePath, CancellationToken ct);
}

public interface IOmrJobQueue
{
    void Enqueue(long sheetId);
}

// Một trang phiếu: thông tin HS và tùy chọn tô sẵn ô SBD / mã đề.
public sealed record OmrSheetCopy(
    string? StudentName,
    string? StudentCode,
    string? ClassName,
    string? ExamCode,
    bool FillStudentCode = true,
    bool FillExamCode = false);

// Vẽ ảnh phiếu trống từ cấu hình mẫu, để xem trước bản in.
public interface IOmrFormRenderer
{
    byte[] Png(OmrTemplateConfig config);

    byte[] Png(OmrTemplateConfig config, OmrSheetCopy copy);

    byte[] Pdf(OmrTemplateConfig config, IReadOnlyList<OmrSheetCopy> copies);
}
