using AIQuizHSG.Application.Accounts;
using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Difficulty;
using AIQuizHSG.Application.Exams;
using AIQuizHSG.Application.Organization;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Application.Validation;
using FluentValidation;
using AIQuizHSG.Infrastructure.Data;
using AIQuizHSG.Infrastructure.Persistence;
using AIQuizHSG.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure;

namespace AIQuizHSG.Infrastructure;

public static class DependencyInjection
{
    // Nối MySQL, kiểm tra khóa JWT, rồi đăng ký Unit of Work và service theo vòng đời request.
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var connectionString = config.GetConnectionString("Default")
            ?? throw new InvalidOperationException("Thiếu chuỗi kết nối ConnectionStrings:Default.");
        var version = config["MySql:Version"] ?? "8.0.36";
        var jwt = config.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
        if (string.IsNullOrWhiteSpace(jwt.SigningKey) || jwt.SigningKey.Length < 32)
            throw new InvalidOperationException("Jwt:SigningKey phải có ít nhất 32 ký tự.");

        services.Configure<JwtOptions>(config.GetSection(JwtOptions.SectionName));
        // Khóa phiên bản server để Pomelo không cần hỏi MySQL lúc khởi động. Mất kết nối thì thử lại 5 lần.
        services.AddDbContext<AiQuizDbContext>(options =>
            options.UseMySql(
                connectionString,
                new MySqlServerVersion(Version.Parse(version)),
                mysql => mysql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(5), null)));

        // Repository không đăng ký riêng. Service chỉ nhận Unit of Work, dùng chung một DbContext.
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<IClock, SystemClock>();
        services.AddSingleton<IPasswordHasher, PasswordHasherService>();
        services.AddSingleton<ITokenIssuer, JwtTokenIssuer>();
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();
        services.AddScoped<ILoginService, LoginService>();
        services.AddScoped<IAccountService, AccountService>();
        services.AddScoped<ICatalogService, CatalogService>();
        services.AddScoped<IQuestionService, QuestionService>();
        services.AddScoped<IExamService, ExamService>();
        services.AddScoped<IAttemptService, AttemptService>();
        services.AddScoped<IExamProfileService, ExamProfileService>();
        services.AddScoped<IExamGenerationService, ExamGenerationService>();
        services.AddScoped<ISchoolAccess, SchoolAccess>();
        services.AddScoped<ISchoolService, SchoolService>();
        services.AddScoped<IClassroomService, ClassroomService>();
        services.AddScoped<IStudentRecordService, StudentRecordService>();
        services.AddScoped<IPermissionAdminService, PermissionAdminService>();
        services.AddScoped<IExamAssignmentService, ExamAssignmentService>();
        services.AddScoped<IClassroomReportService, ClassroomReportService>();
        return services;
    }
}
