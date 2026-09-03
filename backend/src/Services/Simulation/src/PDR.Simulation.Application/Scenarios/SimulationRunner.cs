using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.Simulation.Application.Upstream;
using PDR.Simulation.Domain.Scenarios;

namespace PDR.Simulation.Application.Scenarios;

/// <summary>
/// Turns a scenario definition plus the current portfolio into a stored run. The arithmetic lives here
/// rather than in a handler so it can be unit tested against fixed snapshots, and so the three modes stay
/// visibly consistent: the future world is the current population under the post-cutover rules, and the
/// remediated world is the future world minus what remediation has already fixed (FR-SIM-001).
/// </summary>
public sealed class SimulationRunner(IPortfolioGateway portfolio, IRemediationGateway remediation, IClock clock)
{
    public async Task<Result<SimulationRun>> ExecuteAsync(
        Scenario scenario,
        string requestedBy,
        CancellationToken cancellationToken)
    {
        if (!scenario.IsRunnable)
        {
            return Result.Failure<SimulationRun>(ScenarioErrors.Archived(scenario.Code));
        }

        var snapshot = await portfolio.GetSnapshotAsync(cancellationToken);
        var remediated = scenario.Mode == ScenarioMode.Remediated
            ? await remediation.GetSnapshotAsync(cancellationToken)
            : null;

        var run = SimulationRun.Start(
            scenario.Id,
            scenario.Code,
            scenario.Mode,
            scenario.AsOf,
            BuildRunKey(scenario, snapshot.RulesetVersion),
            requestedBy,
            clock.UtcNow);

        var rejected = scenario.Mode switch
        {
            ScenarioMode.Current => snapshot.CurrentRejectedCount,
            ScenarioMode.Future => snapshot.FutureRejectedCount,
            _ => Math.Max(snapshot.FutureRejectedCount - FixedByRemediation(remediated), 0)
        };

        var paymentsAtRisk = scenario.Mode switch
        {
            ScenarioMode.Current => snapshot.CurrentRejectedCount,
            ScenarioMode.Future => snapshot.PaymentsAtRisk,
            _ => remediated?.FutureExposureOpen ?? snapshot.PaymentsAtRisk
        };

        var scoped = await ScopedRowsAsync(scenario, cancellationToken);
        var warnings = scoped.Sum(row =>
            scenario.Mode == ScenarioMode.Current ? row.CurrentWarningCount : row.FutureWarningCount);

        var completion = run.Complete(
            snapshot.AssessedCount + snapshot.ExcludedCount + snapshot.UnableToAssessCount,
            snapshot.AssessedCount,
            snapshot.ExcludedCount,
            snapshot.UnableToAssessCount,
            rejected,
            warnings,
            paymentsAtRisk,
            snapshot.RulesetVersion,
            clock.UtcNow);

        if (completion.IsFailure)
        {
            return Result.Failure<SimulationRun>(completion.Error);
        }

        foreach (var row in scoped)
        {
            run.AddBreakdown(
                ParseDimension(row.Dimension),
                row.Key,
                row.RecordCount,
                RowRejected(scenario.Mode, row, remediated, snapshot),
                scenario.Mode == ScenarioMode.Current ? row.CurrentWarningCount : row.FutureWarningCount,
                scenario.Mode == ScenarioMode.Current ? row.CurrentRejectedCount : row.FutureRejectedCount);
        }

        return Result.Success(run);
    }

    /// <summary>
    /// Approved-but-not-yet-written-back corrections count as fixed in the remediated view, because that view
    /// answers "where do we land if everything we have agreed to actually lands".
    /// </summary>
    private static int FixedByRemediation(RemediationSnapshot? snapshot) =>
        snapshot is null ? 0 : snapshot.RemediatedCases + snapshot.ApprovedCases;

    /// <summary>
    /// The remediated view thins each row in proportion to what has been fixed overall; per-row remediation
    /// counts are not published by the remediation service, so a row is never claimed better than the portfolio.
    /// </summary>
    private static int RowRejected(
        ScenarioMode mode,
        PortfolioProfileRow row,
        RemediationSnapshot? remediated,
        PortfolioSnapshot snapshot)
    {
        if (mode == ScenarioMode.Current)
        {
            return row.CurrentRejectedCount;
        }

        if (mode == ScenarioMode.Future || remediated is null || snapshot.FutureRejectedCount == 0)
        {
            return row.FutureRejectedCount;
        }

        var remaining = Math.Max(snapshot.FutureRejectedCount - FixedByRemediation(remediated), 0);
        var ratio = (decimal)remaining / snapshot.FutureRejectedCount;
        return (int)Math.Round(row.FutureRejectedCount * ratio, MidpointRounding.AwayFromZero);
    }

    private async Task<IReadOnlyList<PortfolioProfileRow>> ScopedRowsAsync(
        Scenario scenario,
        CancellationToken cancellationToken)
    {
        var rows = new List<PortfolioProfileRow>();

        foreach (var dimension in new[] { "Scheme", "Source", "Country", "Issue" })
        {
            var profile = await portfolio.GetProfileAsync(dimension, cancellationToken);
            rows.AddRange(profile.Where(row => InScope(scenario, dimension, row.Key)));
        }

        return rows;
    }

    private static bool InScope(Scenario scenario, string dimension, string key) => dimension switch
    {
        "Scheme" => Matches(scenario.SchemeCodes, key),
        "Source" => Matches(scenario.SourceCodes, key),
        "Country" => Matches(scenario.Countries, key),
        _ => true
    };

    private static bool Matches(string? csv, string key) =>
        string.IsNullOrWhiteSpace(csv) ||
        csv.Split(',').Contains(key.ToUpperInvariant(), StringComparer.OrdinalIgnoreCase);

    private static BreakdownDimension ParseDimension(string dimension) =>
        Enum.TryParse<BreakdownDimension>(dimension, ignoreCase: true, out var parsed)
            ? parsed
            : BreakdownDimension.Scheme;

    /// <summary>
    /// Identifies the definition a run was produced from, so an identical re-run is recognisable and a run
    /// whose ruleset moved on is not silently compared with an older one (FR-SIM-002).
    /// </summary>
    public static string BuildRunKey(Scenario scenario, string? rulesetVersion)
    {
        var definition = string.Join(
            '|',
            scenario.Code,
            scenario.Mode,
            scenario.AsOf.ToString("O", CultureInfo.InvariantCulture),
            scenario.SchemeCodes ?? "*",
            scenario.SourceCodes ?? "*",
            scenario.Countries ?? "*",
            scenario.PartyRoles ?? "*",
            scenario.Exclusions ?? "-",
            rulesetVersion ?? scenario.RulesetVersion ?? "-");

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(definition));
        return $"{scenario.Code}:{Convert.ToHexStringLower(hash)[..32]}";
    }
}
