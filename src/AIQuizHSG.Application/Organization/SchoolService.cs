using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

namespace AIQuizHSG.Application.Organization;

// Trường, năm học, khối và thành viên. Mã học sinh sẽ gắn về trường này.
public sealed partial class SchoolService : TimedOrganizationService, ISchoolService
{
    private readonly IValidator<SaveSchoolRequest> schoolValidator;
    private readonly IValidator<SaveSchoolYearRequest> yearValidator;
    private readonly IValidator<SaveGradeLevelRequest> gradeValidator;

    public SchoolService(
        IUnitOfWork unitOfWork,
        IClock clock,
        ISchoolAccess access,
        IValidator<SaveSchoolRequest> schoolValidator,
        IValidator<SaveSchoolYearRequest> yearValidator,
        IValidator<SaveGradeLevelRequest> gradeValidator) : base(unitOfWork, clock, access)
    {
        this.schoolValidator = schoolValidator;
        this.yearValidator = yearValidator;
        this.gradeValidator = gradeValidator;
    }

    public async Task<OperationResult<IReadOnlyList<SchoolResponse>>> ListAsync(Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "school.view", null, ct) is { } denied)
            return OperationResult<IReadOnlyList<SchoolResponse>>.Forbidden(denied);

        var schools = await _unitOfWork.Schools.ListAsync(ct);
        var user = await _unitOfWork.Users.FindByIdAsync(actor.UserId, ct);
        if (user is not null && user.UserRoles.All(link => link.Role.Code != RoleCodes.Admin))
        {
            var visible = new List<School>();
            foreach (var school in schools)
            {
                if (await _unitOfWork.Schools.HasActiveMembershipAsync(actor.UserId, school.Id, ct))
                    visible.Add(school);
            }

            schools = visible;
        }

        return OperationResult<IReadOnlyList<SchoolResponse>>.Ok(schools.Select(OrganizationMapper.ToSchool).ToArray());
    }

    public async Task<OperationResult<SchoolResponse>> GetAsync(long id, Actor actor, CancellationToken ct)
    {
        var school = await _unitOfWork.Schools.FindAsync(id, ct);
        if (school is null)
            return OperationResult<SchoolResponse>.NotFound(Messages.SchoolNotFound);
        if (await access.DenyAsync(actor.UserId, "school.view", id, ct) is { } denied)
            return Forbid<SchoolResponse>(denied);
        return OperationResult<SchoolResponse>.Ok(OrganizationMapper.ToSchool(school));
    }

    public async Task<OperationResult<SchoolResponse>> CreateAsync(SaveSchoolRequest request, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "school.manage", null, ct) is { } denied)
            return Forbid<SchoolResponse>(denied);
        if (await schoolValidator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<SchoolResponse>.Invalid(error);

        var code = OrganizationMapper.Code(request.Code);
        if (await _unitOfWork.Schools.FindByCodeAsync(code, ct) is not null)
            return OperationResult<SchoolResponse>.Invalid(Messages.SchoolCodeExists);

        var now = clock.UtcNow;
        var school = new School
        {
            Code = code,
            Name = request.Name.Trim(),
            Address = OrganizationMapper.Empty(request.Address),
            ProvinceCode = OrganizationMapper.Empty(request.ProvinceCode),
            DistrictCode = OrganizationMapper.Empty(request.DistrictCode),
            Phone = OrganizationMapper.Empty(request.Phone),
            Email = OrganizationMapper.Empty(request.Email),
            IsActive = request.IsActive,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        await _unitOfWork.Schools.AddAsync(school, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateSchool, AuditCodes.School, () => school.Id, school.Code, actor.IpAddress, ct);
        return OperationResult<SchoolResponse>.Ok(OrganizationMapper.ToSchool(school));
    }

    public async Task<OperationResult<SchoolResponse>> UpdateAsync(long id, SaveSchoolRequest request, Actor actor, CancellationToken ct)
    {
        var school = await _unitOfWork.Schools.FindAsync(id, ct);
        if (school is null)
            return OperationResult<SchoolResponse>.NotFound(Messages.SchoolNotFound);
        if (await access.DenyAsync(actor.UserId, "school.manage", id, ct) is { } denied)
            return Forbid<SchoolResponse>(denied);
        if (await schoolValidator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<SchoolResponse>.Invalid(error);

        var code = OrganizationMapper.Code(request.Code);
        var existing = await _unitOfWork.Schools.FindByCodeAsync(code, ct);
        if (existing is not null && existing.Id != id)
            return OperationResult<SchoolResponse>.Invalid(Messages.SchoolCodeExists);

        school.Code = code;
        school.Name = request.Name.Trim();
        school.Address = OrganizationMapper.Empty(request.Address);
        school.ProvinceCode = OrganizationMapper.Empty(request.ProvinceCode);
        school.DistrictCode = OrganizationMapper.Empty(request.DistrictCode);
        school.Phone = OrganizationMapper.Empty(request.Phone);
        school.Email = OrganizationMapper.Empty(request.Email);
        school.IsActive = request.IsActive;
        school.UpdatedAtUtc = clock.UtcNow;
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateSchool, AuditCodes.School, () => school.Id, school.Code, actor.IpAddress, ct);
        return OperationResult<SchoolResponse>.Ok(OrganizationMapper.ToSchool(school));
    }

    public async Task<OperationResult<IReadOnlyList<SchoolYearResponse>>> ListYearsAsync(long schoolId, Actor actor, CancellationToken ct)
    {
        if (await _unitOfWork.Schools.FindAsync(schoolId, ct) is null)
            return OperationResult<IReadOnlyList<SchoolYearResponse>>.NotFound(Messages.SchoolNotFound);
        if (await access.DenyAsync(actor.UserId, "school.view", schoolId, ct) is { } denied)
            return Forbid<IReadOnlyList<SchoolYearResponse>>(denied);
        var years = await _unitOfWork.Schools.ListYearsAsync(schoolId, ct);
        return OperationResult<IReadOnlyList<SchoolYearResponse>>.Ok(years.Select(OrganizationMapper.ToYear).ToArray());
    }

    public async Task<OperationResult<SchoolYearResponse>> CreateYearAsync(long schoolId, SaveSchoolYearRequest request, Actor actor, CancellationToken ct)
    {
        if (await _unitOfWork.Schools.FindAsync(schoolId, ct) is null)
            return OperationResult<SchoolYearResponse>.NotFound(Messages.SchoolNotFound);
        if (await access.DenyAsync(actor.UserId, "school.manage", schoolId, ct) is { } denied)
            return Forbid<SchoolYearResponse>(denied);
        if (await yearValidator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<SchoolYearResponse>.Invalid(error);

        var name = request.Name.Trim();
        if (await _unitOfWork.Schools.FindYearByNameAsync(schoolId, name, ct) is not null)
            return OperationResult<SchoolYearResponse>.Invalid(Messages.SchoolYearNameExists);

        if (request.IsCurrent)
            await _unitOfWork.Schools.ClearCurrentAsync(schoolId, null, ct);

        var year = new SchoolYear
        {
            SchoolId = schoolId,
            Name = name,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsCurrent = request.IsCurrent,
            Status = request.IsCurrent ? SchoolYearStatus.Open : request.Status
        };
        await _unitOfWork.Schools.AddYearAsync(year, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateSchoolYear, AuditCodes.SchoolYear, () => year.Id, year.Name, actor.IpAddress, ct);
        return OperationResult<SchoolYearResponse>.Ok(OrganizationMapper.ToYear(year));
    }

    public async Task<OperationResult<SchoolYearResponse>> UpdateYearAsync(long id, SaveSchoolYearRequest request, Actor actor, CancellationToken ct)
    {
        var year = await _unitOfWork.Schools.FindYearAsync(id, ct);
        if (year is null)
            return OperationResult<SchoolYearResponse>.NotFound(Messages.SchoolYearNotFound);
        if (await access.DenyAsync(actor.UserId, "school.manage", year.SchoolId, ct) is { } denied)
            return Forbid<SchoolYearResponse>(denied);
        if (await yearValidator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<SchoolYearResponse>.Invalid(error);

        var name = request.Name.Trim();
        var sameName = await _unitOfWork.Schools.FindYearByNameAsync(year.SchoolId, name, ct);
        if (sameName is not null && sameName.Id != id)
            return OperationResult<SchoolYearResponse>.Invalid(Messages.SchoolYearNameExists);

        if (request.IsCurrent)
            await _unitOfWork.Schools.ClearCurrentAsync(year.SchoolId, id, ct);

        year.Name = name;
        year.StartDate = request.StartDate;
        year.EndDate = request.EndDate;
        year.IsCurrent = request.IsCurrent;
        year.Status = request.Status;
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateSchoolYear, AuditCodes.SchoolYear, () => year.Id, year.Name, actor.IpAddress, ct);
        return OperationResult<SchoolYearResponse>.Ok(OrganizationMapper.ToYear(year));
    }

    public async Task<OperationResult<IReadOnlyList<GradeLevelResponse>>> ListGradesAsync(Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "class.view", null, ct) is { } denied)
            return Forbid<IReadOnlyList<GradeLevelResponse>>(denied);
        var levels = await _unitOfWork.GradeLevels.ListAsync(ct);
        return OperationResult<IReadOnlyList<GradeLevelResponse>>.Ok(levels.Select(OrganizationMapper.ToGrade).ToArray());
    }

    public async Task<OperationResult<GradeLevelResponse>> CreateGradeAsync(SaveGradeLevelRequest request, Actor actor, CancellationToken ct)
    {
        if (await access.DenyAsync(actor.UserId, "school.manage", null, ct) is { } denied)
            return Forbid<GradeLevelResponse>(denied);
        if (await gradeValidator.FirstErrorAsync(request, ct) is { } error)
            return OperationResult<GradeLevelResponse>.Invalid(error);

        var code = OrganizationMapper.Code(request.Code);
        if (await _unitOfWork.GradeLevels.FindByCodeAsync(code, ct) is not null)
            return OperationResult<GradeLevelResponse>.Invalid(Messages.GradeLevelCodeExists);

        var level = new GradeLevel
        {
            Code = code,
            Name = request.Name.Trim(),
            EducationLevel = string.IsNullOrWhiteSpace(request.EducationLevel) ? "THCS" : request.EducationLevel.Trim(),
            SortOrder = request.SortOrder,
            IsActive = request.IsActive
        };
        await _unitOfWork.GradeLevels.AddAsync(level, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateGradeLevel, AuditCodes.GradeLevel, () => level.Id, level.Code, actor.IpAddress, ct);
        return OperationResult<GradeLevelResponse>.Ok(OrganizationMapper.ToGrade(level));
    }

    public async Task<OperationResult<SchoolMemberResponse>> AddMemberAsync(long schoolId, AddSchoolMemberRequest request, Actor actor, CancellationToken ct)
    {
        if (await _unitOfWork.Schools.FindAsync(schoolId, ct) is null)
            return OperationResult<SchoolMemberResponse>.NotFound(Messages.SchoolNotFound);
        if (await access.DenyAsync(actor.UserId, "school.manage", schoolId, ct) is { } denied)
            return Forbid<SchoolMemberResponse>(denied);

        var user = await _unitOfWork.Users.FindByIdAsync(request.UserId, ct);
        if (user is null || user.Status != UserStatus.Active)
            return OperationResult<SchoolMemberResponse>.Invalid(Messages.AccountInactive);
        if (await _unitOfWork.Schools.FindMembershipAsync(schoolId, request.UserId, request.SchoolRole, ct) is not null)
            return OperationResult<SchoolMemberResponse>.Invalid(Messages.TeacherAssignmentExists);

        var membership = new SchoolMembership
        {
            SchoolId = schoolId,
            UserId = request.UserId,
            SchoolRole = request.SchoolRole,
            Status = MembershipStatus.Active,
            StartDate = request.StartDate ?? DateOnly.FromDateTime(clock.UtcNow)
        };
        await _unitOfWork.Schools.AddMembershipAsync(membership, ct);
        await _unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.AddSchoolMember, AuditCodes.School, () => membership.Id, request.SchoolRole.ToString(), actor.IpAddress, ct);
        return OperationResult<SchoolMemberResponse>.Ok(new SchoolMemberResponse(
            membership.Id, user.Id, user.FullName, user.Username, membership.SchoolRole, membership.Status));
    }

    public async Task<OperationResult<IReadOnlyList<SchoolMemberResponse>>> ListMembersAsync(long schoolId, Actor actor, CancellationToken ct)
    {
        if (await _unitOfWork.Schools.FindAsync(schoolId, ct) is null)
            return OperationResult<IReadOnlyList<SchoolMemberResponse>>.NotFound(Messages.SchoolNotFound);
        if (await access.DenyAsync(actor.UserId, "school.view", schoolId, ct) is { } denied)
            return Forbid<IReadOnlyList<SchoolMemberResponse>>(denied);
        var members = await _unitOfWork.Schools.ListMembershipsAsync(schoolId, ct);
        return OperationResult<IReadOnlyList<SchoolMemberResponse>>.Ok(members.Select(item =>
            new SchoolMemberResponse(item.Id, item.UserId, item.User.FullName, item.User.Username, item.SchoolRole, item.Status)).ToArray());
    }

    private static OperationResult<T> Forbid<T>(string message) => OperationResult<T>.Forbidden(message);
}
