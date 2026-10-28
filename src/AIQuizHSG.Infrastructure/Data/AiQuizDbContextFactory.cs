using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace AIQuizHSG.Infrastructure.Data;

// Chỉ dùng cho lệnh `dotnet ef`. Ứng dụng đang chạy đọc chuỗi kết nối trong appsettings.
public class AiQuizDbContextFactory : IDesignTimeDbContextFactory<AiQuizDbContext>
{
    public AiQuizDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<AiQuizDbContext>()
            .UseMySql(
                "Server=127.0.0.1;Port=3306;Database=aiquizhsg;User=root;Password=dev",
                new MySqlServerVersion(new Version(8, 0, 36)))
            .Options;
        return new AiQuizDbContext(options);
    }
}
