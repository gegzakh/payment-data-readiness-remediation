using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Results;
using PDR.BuildingBlocks.Core.Settings;
using PDR.BuildingBlocks.Core.Time;
using PDR.BuildingBlocks.Security;
using PDR.Ingestion.Application.Abstractions;
using PDR.Ingestion.Application.Parsing;
using PDR.Ingestion.Domain.Batches;

namespace PDR.Ingestion.Application.Ingest.Commands;

public sealed record IngestPayloadCommand(
    string SourceCode,
    string FileName,
    IngestionFormat Format,
    IngestionChannel Channel,
    byte[] Content,
    bool Reprocess,
    string? IdempotencyKey) : ICommand<IngestionBatchDto>, IIdempotentCommand;

public sealed record RetryBatchCommand(Guid BatchId) : ICommand<IngestionBatchDto>;

public sealed record CancelBatchCommand(Guid BatchId) : ICommand;

public sealed class IngestPayloadCommandValidator : AbstractValidator<IngestPayloadCommand>
{
    public IngestPayloadCommandValidator()
    {
        RuleFor(command => command.SourceCode).NotEmpty().MaximumLength(32);
        RuleFor(command => command.FileName).NotEmpty().MaximumLength(256);
        RuleFor(command => command.Content).NotEmpty();
        RuleFor(command => command.IdempotencyKey).MaximumLength(128);
    }
}

public sealed class IngestPayloadCommandHandler(
    IIngestionDbContext context,
    FileSafetyInspector inspector,
    BatchProcessor processor,
    IEnumerable<IAddressParser> parsers,
    ISettingsReader settings,
    ICurrentUser currentUser,
    IClock clock) : IRequestHandler<IngestPayloadCommand, Result<IngestionBatchDto>>
{
    public async Task<Result<IngestionBatchDto>> HandleAsync(
        IngestPayloadCommand request,
        CancellationToken cancellationToken)
    {
        var options = await inspector.ResolveOptionsAsync(cancellationToken);
        var inspection = FileSafetyInspector.Inspect(request.FileName, request.Format, request.Content, options);
        var sourceCode = request.SourceCode.ToUpperInvariant();
        var idempotencyKey = string.IsNullOrWhiteSpace(request.IdempotencyKey)
            ? $"{sourceCode}:{inspection.Checksum}:{(request.Reprocess ? Guid.NewGuid().ToString("n") : "once")}"
            : request.IdempotencyKey;

        // Replaying the same key must return the original outcome rather than ingest twice (FR-ING-005).
        var existing = await context.Batches
            .FirstOrDefaultAsync(batch => batch.IdempotencyKey == idempotencyKey, cancellationToken);
        if (existing is not null)
        {
            return existing.ToDto();
        }

        if (!request.Reprocess)
        {
            var duplicate = await context.Batches
                .Where(batch => batch.SourceCode == sourceCode && batch.Checksum == inspection.Checksum)
                .Where(batch => batch.Status != BatchStatus.Quarantined && batch.Status != BatchStatus.Cancelled)
                .Select(batch => new { batch.Id })
                .FirstOrDefaultAsync(cancellationToken);

            if (duplicate is not null)
            {
                return Result.Failure<IngestionBatchDto>(BatchErrors.DuplicateScan(duplicate.Id));
            }
        }

        var parserVersion = parsers.Single(parser => parser.Format == request.Format).Version;

        var batch = IngestionBatch.Receive(
            sourceCode,
            request.FileName,
            request.Format,
            request.Channel,
            request.Content.LongLength,
            inspection.Checksum,
            idempotencyKey,
            parserVersion,
            currentUser.UserName ?? "system",
            request.Reprocess,
            clock.UtcNow);

        context.Batches.Add(batch);

        if (!inspection.IsSafe)
        {
            batch.Quarantine(inspection.RejectionReason!, clock.UtcNow);
            await context.SaveChangesAsync(cancellationToken);
            return batch.ToDto();
        }

        context.Payloads.Add(BatchPayload.Create(batch.Id, request.Content));

        var start = batch.StartParsing(clock.UtcNow);
        if (start.IsFailure)
        {
            return Result.Failure<IngestionBatchDto>(start.Error);
        }

        await context.SaveChangesAsync(cancellationToken);

        var schemeCode = await settings.GetAsync(IngestionSettingKeys.DefaultSchemeCode, "SEPA", cancellationToken);
        await processor.ProcessAsync(batch, request.Content, options, schemeCode, cancellationToken);

        return batch.ToDto();
    }
}

public sealed class RetryBatchCommandHandler(
    IIngestionDbContext context,
    FileSafetyInspector inspector,
    BatchProcessor processor,
    ISettingsReader settings,
    IClock clock) : IRequestHandler<RetryBatchCommand, Result<IngestionBatchDto>>
{
    public async Task<Result<IngestionBatchDto>> HandleAsync(
        RetryBatchCommand request,
        CancellationToken cancellationToken)
    {
        var batch = await context.Batches.FirstOrDefaultAsync(entry => entry.Id == request.BatchId, cancellationToken);
        if (batch is null)
        {
            return Result.Failure<IngestionBatchDto>(BatchErrors.NotFound(request.BatchId));
        }

        var payload = await context.Payloads
            .FirstOrDefaultAsync(entry => entry.BatchId == batch.Id, cancellationToken);
        if (payload is null)
        {
            return Result.Failure<IngestionBatchDto>(BatchErrors.NotRetryable(batch.Status));
        }

        var prepared = batch.PrepareRetry(clock.UtcNow);
        if (prepared.IsFailure)
        {
            return Result.Failure<IngestionBatchDto>(prepared.Error);
        }

        await context.SaveChangesAsync(cancellationToken);

        var options = await inspector.ResolveOptionsAsync(cancellationToken);
        var schemeCode = await settings.GetAsync(IngestionSettingKeys.DefaultSchemeCode, "SEPA", cancellationToken);
        await processor.ProcessAsync(batch, payload.Content, options, schemeCode, cancellationToken);

        return batch.ToDto();
    }
}

public sealed class CancelBatchCommandHandler(IIngestionDbContext context, IClock clock)
    : IRequestHandler<CancelBatchCommand, Result>
{
    public async Task<Result> HandleAsync(CancelBatchCommand request, CancellationToken cancellationToken)
    {
        var batch = await context.Batches.FirstOrDefaultAsync(entry => entry.Id == request.BatchId, cancellationToken);
        if (batch is null)
        {
            return Result.Failure(BatchErrors.NotFound(request.BatchId));
        }

        var result = batch.Cancel(clock.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
