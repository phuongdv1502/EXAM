namespace AIQuizHSG.Domain.Entities;

// Mẫu phiếu đã khóa tọa độ. Đổi tọa độ thì tăng phiên bản, không sửa mẫu đang dùng.
public class OmrTemplate
{
    public long Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public int Version { get; set; } = 1;
    public string ConfigJson { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public long CreatedById { get; set; }

    public User CreatedBy { get; set; } = null!;
}
