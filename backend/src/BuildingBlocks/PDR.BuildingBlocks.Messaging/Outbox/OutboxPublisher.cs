using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PDR.BuildingBlocks.Persistence;

namespace PDR.BuildingBlocks.Messaging.Outbox;

/// <summary>
/// Drains the transactional outbox to RabbitMQ. Failures are retried with an attempt counter, and a row is
/// never deleted, so publication remains auditable (FR-AUD-001).
/// </summary>
public sealed class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    IOptions<MessagingOptions> options,
    ILogger<OutboxPublisher> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Value.Enabled)
        {
            return;
        }

        var interval = TimeSpan.FromSeconds(options.Value.OutboxPollIntervalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PublishPendingAsync(stoppingToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                logger.LogError(exception, "Outbox publication cycle failed");
            }

            try
            {
                await Task.Delay(interval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    private async Task PublishPendingAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<BaseDbContext>();
        var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

        var pending = await context.OutboxMessages
            .Where(message => message.ProcessedAtUtc == null && message.Attempts < options.Value.OutboxMaxAttempts)
            .OrderBy(message => message.OccurredAtUtc)
            .Take(options.Value.OutboxBatchSize)
            .ToListAsync(cancellationToken);

        foreach (var message in pending)
        {
            try
            {
                var type = Type.GetType(message.Type)
                           ?? throw new InvalidOperationException($"Unknown outbox message type {message.Type}");
                var payload = JsonSerializer.Deserialize(message.Payload, type)
                              ?? throw new InvalidOperationException($"Empty payload for {message.Type}");

                await publishEndpoint.Publish(payload, type, context => context.CorrelationId =
                    Guid.TryParse(message.CorrelationId, out var correlationId) ? correlationId : Guid.CreateVersion7(),
                    cancellationToken);

                message.ProcessedAtUtc = DateTimeOffset.UtcNow;
                message.Error = null;
            }
            catch (Exception exception)
            {
                message.Attempts++;
                message.Error = exception.Message[..Math.Min(exception.Message.Length, 4000)];
                logger.LogError(exception, "Failed to publish outbox message {MessageId}", message.Id);
            }
        }

        if (pending.Count > 0)
        {
            await context.SaveChangesAsync(cancellationToken);
        }
    }
}
