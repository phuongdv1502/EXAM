using AIQuizHSG.Api;
using AIQuizHSG.Application.Organization;
using AIQuizHSG.Application.Questions;
using AIQuizHSG.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AIQuizHSG.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/schools")]
// Trường, năm học, thành viên và hồ sơ học sinh theo phạm vi trường.
public class SchoolsController(ISchoolService schools, IStudentRecordService students) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SchoolResponse>>> List(CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await schools.ListAsync(actor, ct));
    }

    [HttpGet("{id:long}")]
    public async Task<ActionResult<SchoolResponse>> Get(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await schools.GetAsync(id, actor, ct));
    }

    [HttpPost]
    public async Task<ActionResult<SchoolResponse>> Create(SaveSchoolRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await schools.CreateAsync(request, actor, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<SchoolResponse>> Update(long id, SaveSchoolRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await schools.UpdateAsync(id, request, actor, ct));
    }

    [HttpGet("{id:long}/school-years")]
    public async Task<ActionResult<IReadOnlyList<SchoolYearResponse>>> Years(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await schools.ListYearsAsync(id, actor, ct));
    }

    [HttpPost("{id:long}/school-years")]
    public async Task<ActionResult<SchoolYearResponse>> CreateYear(long id, SaveSchoolYearRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await schools.CreateYearAsync(id, request, actor, ct));
    }

    [HttpGet("{id:long}/members")]
    public async Task<ActionResult<IReadOnlyList<SchoolMemberResponse>>> Members(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await schools.ListMembersAsync(id, actor, ct));
    }

    [HttpPost("{id:long}/members")]
    public async Task<ActionResult<SchoolMemberResponse>> AddMember(long id, AddSchoolMemberRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await schools.AddMemberAsync(id, request, actor, ct));
    }

    [HttpGet("{id:long}/students")]
    public async Task<ActionResult<IReadOnlyList<StudentProfileResponse>>> Students(long id, [FromQuery] string? keyword, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await students.ListAsync(id, keyword, actor, ct));
    }

    [HttpPost("{id:long}/students")]
    public async Task<ActionResult<StudentProfileResponse>> CreateStudent(long id, SaveStudentProfileRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await students.CreateAsync(id, request, actor, ct));
    }

    [HttpGet("{id:long}/students/import-template")]
    public IActionResult Template(long id) =>
        File(students.CreateImportTemplate(), StudentExcel.ContentType, StudentExcel.FileName);

    [HttpPost("{id:long}/school-years/{yearId:long}/students/import")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<ActionResult<ImportResult>> Import(long id, long yearId, IFormFile? file, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        if (file is null || file.Length == 0)
            return BadRequest(new { message = Messages.ExcelNotSelected });
        if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { message = Messages.ExcelWrongExtension });
        await using var stream = file.OpenReadStream();
        return ApiResults.From(await students.ImportAsync(id, yearId, stream, actor, ct));
    }
}

[ApiController]
[Authorize]
[Route("api/school-years")]
public class SchoolYearsController(ISchoolService schools) : ControllerBase
{
    [HttpPut("{id:long}")]
    public async Task<ActionResult<SchoolYearResponse>> Update(long id, SaveSchoolYearRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await schools.UpdateYearAsync(id, request, actor, ct));
    }
}

[ApiController]
[Authorize]
[Route("api/grade-levels")]
public class GradeLevelsController(ISchoolService schools) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<GradeLevelResponse>>> List(CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await schools.ListGradesAsync(actor, ct));
    }

    [HttpPost]
    public async Task<ActionResult<GradeLevelResponse>> Create(SaveGradeLevelRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await schools.CreateGradeAsync(request, actor, ct));
    }
}

[ApiController]
[Authorize]
[Route("api/classrooms")]
public class ClassroomsController(IClassroomService classrooms, IStudentRecordService students) : ControllerBase
{
    [HttpGet("/api/schools/{schoolId:long}/classrooms")]
    public async Task<ActionResult<IReadOnlyList<ClassroomResponse>>> List(long schoolId, [FromQuery] long? schoolYearId, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await classrooms.ListAsync(schoolId, schoolYearId, actor, ct));
    }

    [HttpPost("/api/schools/{schoolId:long}/classrooms")]
    public async Task<ActionResult<ClassroomResponse>> Create(long schoolId, SaveClassroomRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await classrooms.CreateAsync(schoolId, request, actor, ct));
    }

    [HttpPut("{id:long}")]
    public async Task<ActionResult<ClassroomResponse>> Update(long id, SaveClassroomRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await classrooms.UpdateAsync(id, request, actor, ct));
    }

    [HttpGet("{id:long}/students")]
    public async Task<ActionResult<IReadOnlyList<StudentProfileResponse>>> Students(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await classrooms.ListStudentsAsync(id, actor, ct));
    }

    [HttpPost("{id:long}/students")]
    public async Task<ActionResult<StudentProfileResponse>> Enroll(long id, EnrollStudentRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await students.EnrollAsync(id, request, actor, ct));
    }

    [HttpPost("{id:long}/teachers")]
    public async Task<ActionResult<ClassTeacherResponse>> AssignTeacher(long id, AssignTeacherRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await classrooms.AssignTeacherAsync(id, request, actor, ct));
    }
}

[ApiController]
[Authorize]
[Route("api/student-records")]
public class StudentRecordsController(IStudentRecordService students) : ControllerBase
{
    [HttpPut("{id:long}")]
    public async Task<ActionResult<StudentProfileResponse>> Update(long id, SaveStudentProfileRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await students.UpdateAsync(id, request, actor, ct));
    }

    [HttpPost("{id:long}/transfer")]
    public async Task<ActionResult<StudentProfileResponse>> Transfer(long id, TransferStudentRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await students.TransferAsync(id, request, actor, ct));
    }
}

[ApiController]
[Authorize]
[Route("api/permissions")]
public class PermissionsController(IPermissionAdminService permissions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PermissionResponse>>> List([FromQuery] string? module, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await permissions.ListAsync(module, actor, ct));
    }
}

[ApiController]
[Authorize]
[Route("api/roles")]
public class RolesController(IPermissionAdminService permissions) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> List(CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await permissions.ListRolesAsync(actor, ct));
    }

    [HttpGet("{id:long}/permissions")]
    public async Task<ActionResult<IReadOnlyList<PermissionResponse>>> Permissions(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await permissions.ListForRoleAsync(id, actor, ct));
    }

    [HttpPut("{id:long}/permissions")]
    public async Task<ActionResult<IReadOnlyList<PermissionResponse>>> Replace(long id, ReplacePermissionsRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await permissions.ReplaceRoleAsync(id, request, actor, ct));
    }
}

[ApiController]
[Authorize]
[Route("api/users")]
public class UserRolesController(IPermissionAdminService permissions) : ControllerBase
{
    [HttpGet("{id:long}/roles")]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> Roles(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await permissions.ListUserRolesAsync(id, actor, ct));
    }

    [HttpPut("{id:long}/roles")]
    public async Task<ActionResult<IReadOnlyList<RoleResponse>>> Replace(long id, ReplaceUserRolesRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await permissions.ReplaceUserRolesAsync(id, request, actor, ct));
    }
}

[ApiController]
[Authorize]
[Route("api/exams/{examId:long}/assignments")]
public class ExamAssignmentsController(IExamAssignmentService assignments) : ControllerBase
{
    [HttpPost]
    public async Task<ActionResult<ExamAssignmentResponse>> Create(long examId, AssignExamRequest request, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await assignments.AssignAsync(examId, request, actor, ct));
    }
}

[ApiController]
[Authorize]
[Route("api/reports/classrooms")]
public class ClassroomReportsController(IClassroomReportService reports) : ControllerBase
{
    [HttpGet("{id:long}/exam-results")]
    public async Task<ActionResult<IReadOnlyList<ClassroomExamResult>>> Results(long id, CancellationToken ct)
    {
        if (!Actors.TryGet(HttpContext, out var actor))
            return ApiResults.Unauthorized();
        return ApiResults.From(await reports.ExamResultsAsync(id, actor, ct));
    }
}
