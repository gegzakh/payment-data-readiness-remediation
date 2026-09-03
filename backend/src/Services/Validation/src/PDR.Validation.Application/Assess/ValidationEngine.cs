using Microsoft.Extensions.Logging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Time;
using PDR.Validation.Application.Abstractions;
using PDR.Validation.Application.Upstream;
using PDR.Validation.Domain.Assessments;

namespace PDR.Validation.Application.Assess;

/// <summary>
/// Runs one batch through classification and both rule sets, and persists the run with its assessments
/// and findings. Nothing about the payment beyond the party address is copied here (FR-VAL-002).
/// </summary>
public sealed class ValidationEngine(
    IValidationDbContext context,
    IIngestionGateway ingestion,
    IRulesGateway rules,
    IClock clock,
    ILogger<ValidationEngine> logger)
{
    public async Task<Result<ValidationRun>> RunAsync(
        Guid batchId,
        string defaultSchemeCode,
        DateOnly? asOf,
        CancellationToken cancellationToken)
    {
        IngestedBatch? batch;
        IReadOnlyList<IngestedRecord> records;

        try
        {
            batch = await ingestion.GetBatchAsync(batchId, cancellationToken);
            if (batch is null)
            {
                return Result.Failure<ValidationRun>(ValidationErrors.BatchNotFound(batchId));
            }

            if (!string.Equals(batch.Status, "Parsed", StringComparison.OrdinalIgnoreCase))
            {
                return Result.Failure<ValidationRun>(ValidationErrors.BatchNotParsed(batch.Status));
            }

            records = await ingestion.GetRecordsAsync(batchId, cancellationToken);
        }
        catch (UpstreamException exception)
        {
            logger.LogWarning(exception, "Ingestion could not be read for batch {BatchId}.", batchId);
            return Result.Failure<ValidationRun>(ValidationErrors.UpstreamUnavailable("ingestion"));
        }

        if (records.Count == 0)
        {
            return Result.Failure<ValidationRun>(ValidationErrors.NoRecords);
        }

        var effectiveDate = asOf ?? DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        var schemeCode = records
            .Select(record => record.SchemeCode)
            .FirstOrDefault(code => !string.IsNullOrWhiteSpace(code)) ?? defaultSchemeCode;

        var run = ValidationRun.Start(batchId, batch.SourceCode, schemeCode, effectiveDate, clock.UtcNow);
        context.Runs.Add(run);

        EffectiveRuleset? currentRules;
        EffectiveRuleset? futureRules;

        try
        {
            currentRules = await rules.GetEffectiveRulesetAsync(schemeCode, effectiveDate, RuleMode.Current, cancellationToken);
            futureRules = await rules.GetEffectiveRulesetAsync(schemeCode, effectiveDate, RuleMode.Future, cancellationToken);
        }
        catch (UpstreamException exception)
        {
            logger.LogWarning(exception, "Rules could not be read for scheme {SchemeCode}.", schemeCode);
            run.Fail($"The rule set for scheme '{schemeCode}' could not be read.", clock.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            return run;
        }

        run.RecordRulesetVersions(currentRules?.VersionNumber, futureRules?.VersionNumber);

        var assessments = new List<AddressAssessment>(records.Count);

        foreach (var record in records)
        {
            var snapshot = ToSnapshot(record, batch.SourceCode);
            var assessment = AddressAssessment.Create(run.Id, snapshot);

            if (!record.IsDuplicate)
            {
                Apply(assessment, snapshot, currentRules, RuleMode.Current);
                Apply(assessment, snapshot, futureRules, RuleMode.Future);
            }

            assessment.Conclude(currentRules is not null, futureRules is not null);
            assessments.Add(assessment);
        }

        context.Assessments.AddRange(assessments);
        run.Complete(assessments, clock.UtcNow);

        await context.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Validated batch {BatchId}: {Assessed} assessed, {Rejected} rejected today, {AtRisk} at risk after cutover.",
            batchId,
            run.AssessedCount,
            run.CurrentRejectedCount,
            run.PaymentsAtRisk);

        return run;
    }

    private static void Apply(
        AddressAssessment assessment,
        AddressSnapshot snapshot,
        EffectiveRuleset? ruleset,
        RuleMode mode)
    {
        if (ruleset is null)
        {
            return;
        }

        foreach (var finding in RuleEvaluator.Evaluate(snapshot, ruleset.Rules))
        {
            assessment.AddIssue(
                mode,
                finding.RuleCode,
                finding.Field,
                finding.Severity,
                finding.Message,
                finding.Expected,
                finding.Actual);
        }
    }

    private static AddressSnapshot ToSnapshot(IngestedRecord record, string sourceCode) =>
        new(
            record.Id,
            record.BatchId,
            sourceCode,
            record.Sequence,
            record.MessageId,
            record.EndToEndId,
            record.PartyRole,
            record.PartyName,
            record.Country,
            record.TownName,
            record.PostCode,
            record.StreetName,
            record.BuildingNumber,
            record.AddressLines,
            record.SchemeCode,
            record.IsDuplicate,
            AddressClassifier.Classify(
                record.Country,
                record.TownName,
                record.PostCode,
                record.StreetName,
                record.BuildingNumber,
                record.AddressLines));
}
