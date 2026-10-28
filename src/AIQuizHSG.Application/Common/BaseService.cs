using AIQuizHSG.Application.Authentication;
using AIQuizHSG.Application.Organization;
using AIQuizHSG.Application.Persistence;

namespace AIQuizHSG.Application.Common;

// Phụ thuộc dùng chung. Service nghiệp vụ kế thừa lớp này, không khai báo lại.
public abstract class BaseService
{
    protected readonly IUnitOfWork _unitOfWork;

    protected BaseService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }
}

// Service có ghi thời điểm tạo, sửa hoặc nộp bài.
public abstract class TimedService : BaseService
{
    protected readonly IClock clock;

    protected TimedService(IUnitOfWork unitOfWork, IClock clock) : base(unitOfWork)
    {
        this.clock = clock;
    }
}

// Service kiểm tra quyền và phạm vi trường, lớp.
public abstract class OrganizationService : BaseService
{
    protected readonly ISchoolAccess access;

    protected OrganizationService(IUnitOfWork unitOfWork, ISchoolAccess access) : base(unitOfWork)
    {
        this.access = access;
    }
}

// Service vừa ghi thời điểm vừa kiểm tra phạm vi trường, lớp.
public abstract class TimedOrganizationService : OrganizationService
{
    protected readonly IClock clock;

    protected TimedOrganizationService(IUnitOfWork unitOfWork, IClock clock, ISchoolAccess access) : base(unitOfWork, access)
    {
        this.clock = clock;
    }
}

// Service đăng nhập và tài khoản. Mật khẩu và token dùng chung, không khai báo lại.
public abstract class AccountBaseService : TimedService
{
    protected readonly IPasswordHasher passwords;

    protected AccountBaseService(IUnitOfWork unitOfWork, IPasswordHasher passwords, IClock clock) : base(unitOfWork, clock)
    {
        this.passwords = passwords;
    }
}
