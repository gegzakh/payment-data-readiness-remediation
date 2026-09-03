using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.Validation.Application.Abstractions;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Application.Assess.Queries;

/// <summary>
/// The newest completed run, so remediation can pick up the current picture without knowing run ids.
/// </summary>
public sealed record GetLatestRunQuery : IQuery<ValidationRunDto>;

/// <summary>
/// Unmasked assessments of one run for the remediation service. It is exposed only on the internal
/// route and only to a caller holding the remediation write permission, because a case has to carry the
/// original value a maker must correct (FR-REM-002, FR-VAL-009).
/// </summary>
public sealed record ExportRunAssessmentsQuery(Guid RunId) : IQuery<IReadOnlyList<AddressAssessmentDto>>;

public sealed class GetLatestRunQueryHandler(IValidationDbContext context)
    : IRequestHandler<GetLatestRunQuery, Result<ValidationRunDto>>
{
    public async Task<Result<ValidationRunDto>> HandleAsync(
        GetLatestRunQuery request,
        CancellationToken cancellationToken)
    {
        var run = await context.Runs
            .AsNoTracking()
            .Where(entry => entry.Status == ValidationRunStatus.Completed)
            .OrderByDescending(entry => entry.StartedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);

        return run is null
            ? Result.Failure<ValidationRunDto>(ValidationErrors.RunNotFound(Guid.Empty))
            : run.ToDto();
    }
}

public sealed class ExportRunAssessmentsQueryHandler(IValidationDbContext context)
    : IRequestHandler<ExportRunAssessmentsQuery, Result<IReadOnlyList<AddressAssessmentDto>>>
{
    public async Task<Result<IReadOnlyList<AddressAssessmentDto>>> HandleAsync(
        ExportRunAssessmentsQuery request,
        CancellationToken cancellationToken)
    {
        if (!await context.Runs.AnyAsync(run => run.Id == request.RunId, cancellationToken))
        {
            return Result.Failure<IReadOnlyList<AddressAssessmentDto>>(ValidationErrors.RunNotFound(request.RunId));
        }

        var assessments = await context.Assessments
            .AsNoTracking()
            .Include(assessment => assessment.Issues)
            .Where(assessment => assessment.RunId == request.RunId)
            .Where(assessment => assessment.CurrentOutcome != RecordOutcome.Excluded)
            .OrderBy(assessment => assessment.Sequence)
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<AddressAssessmentDto>>(
            [.. assessments.Select(assessment => assessment.ToDto(unmasked: true))]);
    }
}
