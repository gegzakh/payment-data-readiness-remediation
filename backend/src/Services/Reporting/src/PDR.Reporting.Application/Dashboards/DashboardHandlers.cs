using System.Globalization;
using System.Text;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Paging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.Reporting.Application.Abstractions;
using PDR.Reporting.Domain.Dashboards;

namespace PDR.Reporting.Application.Dashboards;

public sealed record DashboardScopeRequest(
    string? SchemeCodes = null,
    string? SourceCodes = null,
    string? Countries = null,
    string? Exclusions = null,
    DateOnly? AsOf = null)
{
    public DashboardScope ToScope() =>
        DashboardScope.Create(SchemeCodes, SourceCodes, Countries, Exclusions, AsOf);
}

/// <summary>Serves the audience dashboard, rebuilding it when the cached snapshot is past its freshness window.</summary>
public sealed record GetDashboardQuery(DashboardAudience Audience, DashboardScopeRequest Scope, bool Refresh = false)
    : IQuery<DashboardDto>;

public sealed record GetDrillDownQuery(DashboardAudience Audience, string Dimension, DashboardScopeRequest Scope)
    : IQuery<DrillDownDto>;

public sealed record GetSnapshotHistoryQuery(DashboardAudience? Audience, int Page = 1, int? PageSize = null)
    : IQuery<PagedResult<DashboardDto>>;

public sealed record ExportDashboardQuery(DashboardAudience Audience, DashboardScopeRequest Scope)
    : IQuery<DashboardExport>;

public sealed record DashboardExport(string FileName, string ContentType, byte[] Content);

internal static class SnapshotQueries
{
    public static IQueryable<DashboardSnapshot> WithDetail(this DbSet<DashboardSnapshot> snapshots) =>
        snapshots.Include(snapshot => snapshot.Metrics).Include(snapshot => snapshot.Breakdown);
}

/// <summary>
/// Capturing is shared by the dashboard, drill-down and export paths so all three quote the same numbers
/// for the same scope within a freshness window (FR-RPT-002).
/// </summary>
public sealed class SnapshotProvider(
    IReportingDbContext context,
    DashboardFactory factory,
    ISettingsReader settings,
    IClock clock)
{
    public async Task<DashboardSnapshot> GetAsync(
        DashboardAudience audience,
        DashboardScope scope,
        bool refresh,
        CancellationToken cancellationToken)
    {
        var seconds = await settings.GetAsync(
            ReportingSettingKeys.FreshnessSeconds,
            ReportingDefaults.FreshnessSeconds,
            cancellationToken);
        var window = TimeSpan.FromSeconds(Math.Max(seconds, 0));

        if (!refresh)
        {
            var latest = await context.Snapshots
                .WithDetail()
                .Where(snapshot => snapshot.Audience == audience && snapshot.ScopeKey == scope.Key)
                .OrderByDescending(snapshot => snapshot.CapturedAtUtc)
                .FirstOrDefaultAsync(cancellationToken);

            if (latest is not null && latest.IsFreshAt(clock.UtcNow, window))
            {
                return latest;
            }
        }

        var captured = await factory.BuildAsync(audience, scope, cancellationToken);
        context.Snapshots.Add(captured);
        await context.SaveChangesAsync(cancellationToken);
        return captured;
    }
}

public sealed class GetDashboardQueryHandler(SnapshotProvider provider)
    : IRequestHandler<GetDashboardQuery, Result<DashboardDto>>
{
    public async Task<Result<DashboardDto>> HandleAsync(GetDashboardQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await provider.GetAsync(request.Audience, request.Scope.ToScope(), request.Refresh, cancellationToken);
        return snapshot.ToDto();
    }
}

public sealed class GetDrillDownQueryHandler(SnapshotProvider provider)
    : IRequestHandler<GetDrillDownQuery, Result<DrillDownDto>>
{
    public async Task<Result<DrillDownDto>> HandleAsync(GetDrillDownQuery request, CancellationToken cancellationToken)
    {
        var dimension = DashboardFactory.Dimensions
            .FirstOrDefault(item => string.Equals(item, request.Dimension, StringComparison.OrdinalIgnoreCase));

        if (dimension is null)
        {
            return Result.Failure<DrillDownDto>(DashboardErrors.UnknownDimension(request.Dimension));
        }

        var audience = dimension switch
        {
            "Scheme" => DashboardAudience.Scheme,
            "Source" => DashboardAudience.Source,
            "Issue" => DashboardAudience.Operations,
            _ => request.Audience
        };

        var snapshot = await provider.GetAsync(audience, request.Scope.ToScope(), false, cancellationToken);

        var rows = snapshot.Breakdown
            .Where(row => row.Dimension == dimension)
            .OrderByDescending(row => row.RejectedCount)
            .ThenBy(row => row.Key, StringComparer.Ordinal)
            .Select(row => row.ToDto())
            .ToList();

        return new DrillDownDto(
            audience,
            dimension,
            snapshot.ScopeDescription,
            snapshot.CapturedAtUtc,
            snapshot.SourceAsOfUtc,
            snapshot.RulesetVersion,
            snapshot.Reconciliation,
            rows);
    }
}

public sealed class GetSnapshotHistoryQueryHandler(IReportingDbContext context, ISettingsReader settings, IClock clock)
    : IRequestHandler<GetSnapshotHistoryQuery, Result<PagedResult<DashboardDto>>>
{
    public async Task<Result<PagedResult<DashboardDto>>> HandleAsync(
        GetSnapshotHistoryQuery request,
        CancellationToken cancellationToken)
    {
        var configured = await settings.GetAsync(
            ReportingSettingKeys.HistoryPageSize,
            ReportingDefaults.HistoryPageSize,
            cancellationToken);
        var pageSize = Math.Clamp(request.PageSize ?? configured, 1, ReportingDefaults.MaxPageSize);
        var page = Math.Max(request.Page, 1);

        var query = context.Snapshots.WithDetail().AsQueryable();
        if (request.Audience is not null)
        {
            query = query.Where(snapshot => snapshot.Audience == request.Audience);
        }

        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderByDescending(snapshot => snapshot.CapturedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<DashboardDto>(
            [.. items.Select(item => item.ToDto())],
            page,
            pageSize,
            total,
            clock.UtcNow);
    }
}

/// <summary>
/// The export carries the scope, ruleset, freshness and reconciliation state in its header rows, so a
/// spreadsheet that leaves the platform can still be traced back to the run it came from (FR-RPT-002).
/// </summary>
public sealed class ExportDashboardQueryHandler(SnapshotProvider provider)
    : IRequestHandler<ExportDashboardQuery, Result<DashboardExport>>
{
    public async Task<Result<DashboardExport>> HandleAsync(ExportDashboardQuery request, CancellationToken cancellationToken)
    {
        var snapshot = await provider.GetAsync(request.Audience, request.Scope.ToScope(), false, cancellationToken);
        var invariant = CultureInfo.InvariantCulture;
        var builder = new StringBuilder();

        builder.AppendLine(invariant, $"# Dashboard,{snapshot.Audience}");
        builder.AppendLine(invariant, $"# Scope,{Escape(snapshot.ScopeDescription)}");
        builder.AppendLine(invariant, $"# Captured,{snapshot.CapturedAtUtc:O}");
        builder.AppendLine(invariant, $"# Source as of,{snapshot.SourceAsOfUtc?.ToString("O", invariant) ?? "unknown"}");
        builder.AppendLine(invariant, $"# Ruleset,{snapshot.RulesetVersion ?? "unknown"}");
        builder.AppendLine(invariant, $"# Reconciliation,{snapshot.Reconciliation}");
        builder.AppendLine("Section,Key,Label,Value,Unit");

        foreach (var metric in snapshot.Metrics)
        {
            builder.AppendLine(
                invariant,
                $"Metric,{Escape(metric.Key)},{Escape(metric.Label)},{(metric.Unit == MetricUnit.Text ? Escape(metric.Text ?? string.Empty) : metric.Value.ToString(invariant))},{metric.Unit}");
        }

        foreach (var row in snapshot.Breakdown.OrderByDescending(row => row.RejectedCount))
        {
            builder.AppendLine(
                invariant,
                $"Breakdown,{Escape(row.Dimension)},{Escape(row.Key)},{row.RecordCount},{row.RejectedCount}");
        }

        var name = $"{snapshot.Audience.ToString().ToLowerInvariant()}-{snapshot.CapturedAtUtc:yyyyMMddHHmmss}.csv";
        return new DashboardExport(name, "text/csv", Encoding.UTF8.GetBytes(builder.ToString()));
    }

    private static string Escape(string value) =>
        value.Contains(',', StringComparison.Ordinal) ? $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"" : value;
}
