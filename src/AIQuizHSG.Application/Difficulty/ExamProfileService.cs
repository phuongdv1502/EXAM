using AIQuizHSG.Application.Common;
using AIQuizHSG.Application.Persistence;
using AIQuizHSG.Application.Validation;
using AIQuizHSG.Domain.Common;
using AIQuizHSG.Domain.Difficulty;
using AIQuizHSG.Domain.Entities;
using FluentValidation;

namespace AIQuizHSG.Application.Difficulty;

public interface IExamProfileService
{
    Task<IReadOnlyList<ExamProfileResponse>> ListAsync(CancellationToken ct);
    Task<OperationResult<ExamProfileResponse>> GetAsync(long id, CancellationToken ct);
    Task<OperationResult<ExamProfileResponse>> CreateAsync(SaveExamProfileRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<ExamProfileResponse>> UpdateAsync(long id, SaveExamProfileRequest request, Actor actor, CancellationToken ct);
    Task<OperationResult<ProfilePreviewResponse>> PreviewAsync(long id, PreviewProfileRequest request, CancellationToken ct);
}

// Tạo và sửa ma trận độ khó của đề. Bản đã dùng hoặc bản hệ thống thì nhân bản, không ghi đè.
public sealed partial class ExamProfileService : TimedService, IExamProfileService
{
    private readonly IValidator<SaveExamProfileRequest> validator;

    public ExamProfileService(IUnitOfWork unitOfWork, IClock clock, IValidator<SaveExamProfileRequest> validator) : base(unitOfWork, clock)
    {
        this.validator = validator;
    }

    public async Task<IReadOnlyList<ExamProfileResponse>> ListAsync(CancellationToken ct)
    {
        var profiles = await unitOfWork.ExamProfiles.ListAsync(ct);
        return profiles.Select(profile => ToResponse(profile)).ToArray();
    }

    public async Task<OperationResult<ExamProfileResponse>> GetAsync(long id, CancellationToken ct)
    {
        var profile = await unitOfWork.ExamProfiles.FindAsync(id, ct);
        if (profile is null)
            return OperationResult<ExamProfileResponse>.NotFound(Messages.ProfileNotFound);
        return OperationResult<ExamProfileResponse>.Ok(ToResponse(profile));
    }

    public async Task<OperationResult<ExamProfileResponse>> CreateAsync(SaveExamProfileRequest request, Actor actor, CancellationToken ct)
    {
        var built = await BuildAsync(request, ct);
        if (built.Error is not null || built.Levels is null)
            return OperationResult<ExamProfileResponse>.Invalid(built.Error ?? Messages.ProfileDistributionInvalid);

        var now = clock.UtcNow;
        var profile = new ExamDifficultyProfile
        {
            Name = request.Name.Trim(),
            TargetDifficulty = request.TargetDifficulty,
            BaseQuestionCount = request.BaseQuestionCount,
            ScalingMode = built.Mode,
            IsSystemDefault = false,
            Version = 1,
            Status = ProfileStatus.Draft,
            CreatedById = actor.UserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        foreach (var level in built.Levels)
            profile.Levels.Add(level);

        await unitOfWork.ExamProfiles.AddAsync(profile, ct);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.CreateProfile, AuditCodes.DifficultyProfile, () => profile.Id, null, actor.IpAddress, ct);
        return OperationResult<ExamProfileResponse>.Ok(ToResponse(profile, built.Names));
    }

    public async Task<OperationResult<ExamProfileResponse>> UpdateAsync(long id, SaveExamProfileRequest request, Actor actor, CancellationToken ct)
    {
        var built = await BuildAsync(request, ct);
        if (built.Error is not null || built.Levels is null)
            return OperationResult<ExamProfileResponse>.Invalid(built.Error ?? Messages.ProfileDistributionInvalid);

        var profile = await unitOfWork.ExamProfiles.FindAsync(id, ct);
        if (profile is null)
            return OperationResult<ExamProfileResponse>.NotFound(Messages.ProfileNotFound);
        if (profile.Status == ProfileStatus.Archived)
            return OperationResult<ExamProfileResponse>.Invalid(Messages.ProfileNotEditable);

        // Đề đã dùng profile này, hoặc đây là ma trận gốc, thì giữ nguyên bản cũ.
        if (profile.IsSystemDefault || await unitOfWork.ExamProfiles.IsUsedAsync(profile.Id, ct))
            return await CopyAsync(profile, request, built, actor, ct);

        profile.Name = request.Name.Trim();
        profile.TargetDifficulty = request.TargetDifficulty;
        profile.BaseQuestionCount = request.BaseQuestionCount;
        profile.ScalingMode = built.Mode;
        profile.UpdatedAtUtc = clock.UtcNow;
        unitOfWork.ExamProfiles.ReplaceLevels(profile, built.Levels);
        await unitOfWork.SaveWithAuditAsync(actor.UserId, AuditCodes.UpdateProfile, AuditCodes.DifficultyProfile, () => profile.Id, null, actor.IpAddress, ct);
        return OperationResult<ExamProfileResponse>.Ok(ToResponse(profile));
    }

    public async Task<OperationResult<ProfilePreviewResponse>> PreviewAsync(long id, PreviewProfileRequest request, CancellationToken ct)
    {
        var profile = await unitOfWork.ExamProfiles.FindAsync(id, ct);
        if (profile is null)
            return OperationResult<ProfilePreviewResponse>.NotFound(Messages.ProfileNotFound);

        var ordered = profile.Levels.OrderBy(level => level.SortOrder).ThenBy(level => level.DifficultyLevelId).ToList();
        var scaled = ExamMatrixCheck.Scale(profile, ordered, request.QuestionCount);
        if (scaled.Error is not null)
        {
            return OperationResult<ProfilePreviewResponse>.Ok(new ProfilePreviewResponse(
                profile.TargetDifficulty,
                request.QuestionCount,
                ScalingModes.ToCode(profile.ScalingMode),
                [],
                false,
                scaled.Error));
        }

        var lines = scaled.Counts.Select((count, index) => new DistributionLine((byte)(index + 1), count)).ToArray();
        return OperationResult<ProfilePreviewResponse>.Ok(new ProfilePreviewResponse(
            profile.TargetDifficulty,
            request.QuestionCount,
            ScalingModes.ToCode(profile.ScalingMode),
            lines,
            true,
            null));
    }

    private async Task<OperationResult<ExamProfileResponse>> CopyAsync(
        ExamDifficultyProfile source,
        SaveExamProfileRequest request,
        BuiltProfile built,
        Actor actor,
        CancellationToken ct)
    {
        var now = clock.UtcNow;
        var copy = new ExamDifficultyProfile
        {
            Name = request.Name.Trim(),
            TargetDifficulty = request.TargetDifficulty,
            BaseQuestionCount = request.BaseQuestionCount,
            ScalingMode = built.Mode,
            IsSystemDefault = false,
            Version = source.Version + 1,
            Status = ProfileStatus.Draft,
            CreatedById = actor.UserId,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        foreach (var level in built.Levels!)
            copy.Levels.Add(level);

        await unitOfWork.ExamProfiles.AddAsync(copy, ct);
        await unitOfWork.SaveWithAuditAsync(
            actor.UserId,
            AuditCodes.CreateProfile,
            AuditCodes.DifficultyProfile,
            () => copy.Id,
            source.Id.ToString(),
            actor.IpAddress,
            ct);
        return OperationResult<ExamProfileResponse>.Ok(ToResponse(copy, built.Names));
    }

    private async Task<BuiltProfile> BuildAsync(SaveExamProfileRequest request, CancellationToken ct)
    {
        request.DifficultyDistribution ??= [];
        if (await validator.FirstErrorAsync(request, ct) is { } inputError)
            return new BuiltProfile(null, default, inputError, new Dictionary<byte, string>());
        if (!ExamTargetDifficulty.IsValid(request.TargetDifficulty))
            return new BuiltProfile(null, default, Messages.TargetDifficultyInvalid, new Dictionary<byte, string>());
        if (!ScalingModes.TryParse(request.ScalingMode, out var mode))
            return new BuiltProfile(null, default, Messages.ScalingModeInvalid, new Dictionary<byte, string>());

        var counts = request.DifficultyDistribution.Select(item => item.Count).ToArray();
        var levels = request.DifficultyDistribution.Select(item => item.Level).ToArray();
        if (levels.Distinct().Count() != DifficultyDistribution.LevelCount
            || levels.Any(level => !ExamTargetDifficulty.IsValid(level))
            || !DifficultyDistribution.IsCompleteBase(request.BaseQuestionCount, counts))
            return new BuiltProfile(null, default, Messages.ProfileDistributionInvalid, new Dictionary<byte, string>());

        var catalog = await unitOfWork.DifficultyLevels.ListActiveAsync(ct);
        var byId = catalog.ToDictionary(level => level.Id);
        var rows = new List<ExamDifficultyProfileLevel>();
        foreach (var item in request.DifficultyDistribution.OrderBy(item => item.Level))
        {
            if (!byId.ContainsKey(item.Level))
                return new BuiltProfile(null, default, Messages.DifficultyLevelNotFound, new Dictionary<byte, string>());
            rows.Add(new ExamDifficultyProfileLevel
            {
                DifficultyLevelId = item.Level,
                BaseQuestionCount = item.Count,
                Ratio = decimal.Round((decimal)item.Count / request.BaseQuestionCount, 6, MidpointRounding.AwayFromZero),
                SortOrder = item.Level
            });
        }

        return new BuiltProfile(rows, mode, null, byId.ToDictionary(pair => pair.Key, pair => pair.Value.Name));
    }

    private static ExamProfileResponse ToResponse(ExamDifficultyProfile profile, IReadOnlyDictionary<byte, string>? names = null) => new(
        profile.Id,
        profile.Name,
        profile.TargetDifficulty,
        ExamTargetDifficulty.Name(profile.TargetDifficulty),
        profile.BaseQuestionCount,
        ScalingModes.ToCode(profile.ScalingMode),
        profile.IsSystemDefault,
        profile.Version,
        profile.Status,
        profile.Levels
            .OrderBy(level => level.SortOrder)
            .Select(level => new ProfileLevelResponse(
                level.DifficultyLevelId,
                level.DifficultyLevel?.Name
                    ?? (names is not null && names.TryGetValue(level.DifficultyLevelId, out var name) ? name : string.Empty),
                level.BaseQuestionCount,
                level.Ratio))
            .ToArray());

    private sealed record BuiltProfile(
        List<ExamDifficultyProfileLevel>? Levels,
        ScalingMode Mode,
        string? Error,
        IReadOnlyDictionary<byte, string> Names);
}
