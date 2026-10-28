using System.Text;
using System.Text.Json.Serialization;
using AIQuizHSG.Api;
using AIQuizHSG.Api.Security;
using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Infrastructure;
using AIQuizHSG.Infrastructure.Data;
using AIQuizHSG.Infrastructure.Security;
using AIQuizHSG.Infrastructure.Seeding;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Đăng ký MySQL, repository và các service nghiệp vụ.
builder.Services.AddInfrastructure(builder.Configuration);

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException("Thiếu cấu hình Jwt.");
var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:4200"];

// Xác thực bằng JWT. Giữ nguyên tên claim sub/role, không đổi sang kiểu claim dài của .NET.
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwt.Issuer,
            ValidateAudience = true,
            ValidAudience = jwt.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = "unique_name",
            RoleClaimType = "role"
        };
    });

// Quản trị đi được cả policy giáo viên và học sinh.
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(RoleCodes.Admin, policy => policy.RequireRole(RoleCodes.Admin));
    options.AddPolicy(RoleCodes.Teacher, policy => policy.RequireRole(RoleCodes.Teacher, RoleCodes.Admin));
    options.AddPolicy(RoleCodes.Student, policy => policy.RequireRole(RoleCodes.Student, RoleCodes.Admin));
});

builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.WithOrigins(corsOrigins).AllowAnyHeader().AllowAnyMethod());
});

// FluentValidation chặn request sai trước khi vào action. Enum trả về chuỗi, không phải số.
builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>())
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "AI Quiz HSG",
        Version = "v1",
        Description = "API thi trắc nghiệm bồi dưỡng học sinh giỏi Tin học THCS."
    });

    // Nút Authorize trên Swagger nhận token thô, không kèm chữ Bearer.
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Dán token JWT lấy từ POST /api/login. Không thêm chữ Bearer."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });

    options.SchemaFilter<StringEnumSchemaFilter>();
});

var app = builder.Build();

// Lần chạy đầu: áp migration, rồi tạo vai trò, tài khoản quản trị và môn TIN nếu chưa có.
try
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<AiQuizDbContext>();
    var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
    var passwords = scope.ServiceProvider.GetRequiredService<IPasswordHasher>();
    await DatabaseSeeder.SeedAsync(db, unitOfWork, passwords, app.Configuration, app.Logger, CancellationToken.None);
}
catch (Exception ex)
{
    app.Logger.LogCritical(
        ex,
        "Không khởi tạo được cơ sở dữ liệu MySQL. Kiểm tra ConnectionStrings:Default và dịch vụ MySQL 8.");
    throw;
}

// Swagger chỉ mở khi chạy Development.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "AI Quiz HSG v1");
        options.DocumentTitle = "AI Quiz HSG";
    });
}

app.UseCors("Frontend");
app.UseAuthentication();
app.UseAuthorization();
// JWT còn hạn nhưng tài khoản đã khóa thì vẫn từ chối.
app.UseMiddleware<ActiveAccountMiddleware>();
app.MapControllers();
app.Run();
