using AIQuizHSG.Application.Organization;
using AIQuizHSG.Domain.Common;
using FluentValidation;

namespace AIQuizHSG.Application.Validation;

public sealed class SaveSchoolRequestValidator : AbstractValidator<SaveSchoolRequest>
{
    public SaveSchoolRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Code).NotEmpty().WithMessage(Messages.SchoolCodeRequired)
            .Must(code => code.Trim().Length <= 50).WithMessage(Messages.SchoolCodeRequired);
        RuleFor(x => x.Name).NotEmpty().WithMessage(Messages.SchoolNameRequired)
            .Must(name => name.Trim().Length <= 255).WithMessage(Messages.SchoolNameRequired);
        RuleFor(x => x.Address).Must(value => value is null || value.Trim().Length <= 500).WithMessage(Messages.DescriptionTooLong);
        RuleFor(x => x.Phone).Must(value => value is null || value.Trim().Length <= 30).WithMessage(Messages.DescriptionTooLong);
        RuleFor(x => x.Email).Must(value => value is null || value.Trim().Length <= 255).WithMessage(Messages.DescriptionTooLong);
    }
}

public sealed class SaveSchoolYearRequestValidator : AbstractValidator<SaveSchoolYearRequest>
{
    public SaveSchoolYearRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Name).NotEmpty().WithMessage(Messages.SchoolYearNameRequired)
            .Must(name => name.Trim().Length <= 30).WithMessage(Messages.SchoolYearNameRequired);
        RuleFor(x => x.EndDate).GreaterThanOrEqualTo(x => x.StartDate).WithMessage(Messages.SchoolYearDatesInvalid);
    }
}

public sealed class SaveGradeLevelRequestValidator : AbstractValidator<SaveGradeLevelRequest>
{
    public SaveGradeLevelRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Code).NotEmpty().WithMessage(Messages.GradeLevelNotFound);
        RuleFor(x => x.Name).NotEmpty().WithMessage(Messages.GradeLevelNotFound);
        RuleFor(x => x.SortOrder).GreaterThan(0).WithMessage(Messages.GradeLevelNotFound);
    }
}

public sealed class SaveClassroomRequestValidator : AbstractValidator<SaveClassroomRequest>
{
    public SaveClassroomRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.SchoolYearId).GreaterThan(0).WithMessage(Messages.SchoolYearNotFound);
        RuleFor(x => x.GradeLevelId).GreaterThan(0).WithMessage(Messages.GradeLevelNotFound);
        RuleFor(x => x.ClassCode).NotEmpty().WithMessage(Messages.ClassroomCodeRequired)
            .Must(code => code.Trim().Length <= 30).WithMessage(Messages.ClassroomCodeRequired);
        RuleFor(x => x.Name).NotEmpty().WithMessage(Messages.ClassroomCodeRequired)
            .Must(name => name.Trim().Length <= 100).WithMessage(Messages.ClassroomCodeRequired);
        RuleFor(x => x.Capacity).Must(value => value is null or > 0).WithMessage(Messages.ClassroomCapacityInvalid);
    }
}

public sealed class SaveStudentProfileRequestValidator : AbstractValidator<SaveStudentProfileRequest>
{
    public SaveStudentProfileRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.StudentCode).NotEmpty().WithMessage(Messages.StudentCodeRequired)
            .Must(code => code.Trim().Length <= 50).WithMessage(Messages.StudentCodeRequired);
        RuleFor(x => x.FullName).NotEmpty().WithMessage(Messages.StudentNameRequired)
            .Must(name => name.Trim().Length <= 255).WithMessage(Messages.StudentNameRequired);
        RuleFor(x => x.Email).Must(value => value is null || value.Trim().Length <= 255).WithMessage(Messages.DescriptionTooLong);
        RuleFor(x => x.Phone).Must(value => value is null || value.Trim().Length <= 30).WithMessage(Messages.DescriptionTooLong);
    }
}

public sealed class AssignExamRequestValidator : AbstractValidator<AssignExamRequest>
{
    public AssignExamRequestValidator()
    {
        RuleLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x).Must(x => x.ClassroomId is not null || x.StudentId is not null)
            .WithMessage(Messages.AssignmentTargetRequired);
        RuleFor(x => x).Must(x => x.ClassroomId is null || x.StudentId is null)
            .WithMessage(Messages.AssignmentTargetConflict);
    }
}

public sealed class ReplacePermissionsRequestValidator : AbstractValidator<ReplacePermissionsRequest>
{
    public ReplacePermissionsRequestValidator()
    {
        RuleFor(x => x.PermissionIds).NotNull().WithMessage(Messages.PermissionNotFound);
    }
}

public sealed class ReplaceUserRolesRequestValidator : AbstractValidator<ReplaceUserRolesRequest>
{
    public ReplaceUserRolesRequestValidator()
    {
        RuleFor(x => x.RoleIds).NotNull().WithMessage(Messages.RoleNotFound);
    }
}
