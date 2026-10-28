using System.Security.Cryptography;
using AIQuizHSG.Application.Omr;
using Microsoft.Extensions.Hosting;

namespace AIQuizHSG.Infrastructure.Omr;

public sealed class OmrFileStore(IHostEnvironment environment) : IOmrFileStore
{
    private string Root => Path.GetFullPath(Path.Combine(environment.ContentRootPath, "storage", "omr"));

    public async Task<OmrStoredFile> SaveAsync(long batchId, string fileName, ReadOnlyMemory<byte> content, CancellationToken ct)
    {
        var hash = Convert.ToHexString(SHA256.HashData(content.Span));
        var extension = Extension(content.Span);
        var relative = $"{batchId}/{hash}{extension}";
        var full = Resolve(relative);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        await File.WriteAllBytesAsync(full, content.ToArray(), ct);
        return new OmrStoredFile(relative, hash, ContentType(extension));
    }

    public async Task<byte[]?> ReadAsync(string relativePath, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
            return null;
        var full = Resolve(relativePath);
        if (!File.Exists(full))
            return null;
        return await File.ReadAllBytesAsync(full, ct);
    }

    // Chặn đường dẫn nhảy ra ngoài thư mục lưu phiếu.
    private string Resolve(string relativePath)
    {
        var cleaned = relativePath.Replace('\\', '/').TrimStart('/');
        if (cleaned.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Đường dẫn phiếu không hợp lệ.");
        var full = Path.GetFullPath(Path.Combine(Root, cleaned.Replace('/', Path.DirectorySeparatorChar)));
        var root = Root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!full.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Đường dẫn phiếu không hợp lệ.");
        return full;
    }

    private static string Extension(ReadOnlySpan<byte> content)
    {
        if (content.Length >= 4 && content[0] == 0x89 && content[1] == 0x50)
            return ".png";
        if (content.Length >= 3 && content[0] == 0xFF && content[1] == 0xD8)
            return ".jpg";
        if (content.Length >= 12 && content[0] == (byte)'R')
            return ".webp";
        return ".bin";
    }

    private static string ContentType(string extension) => extension switch
    {
        ".png" => "image/png",
        ".jpg" => "image/jpeg",
        ".webp" => "image/webp",
        _ => "application/octet-stream"
    };
}
