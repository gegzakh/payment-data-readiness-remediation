using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PDR.BuildingBlocks.Application.Messaging;
using PDR.BuildingBlocks.Core.Errors;
using PDR.BuildingBlocks.Core.Results;

namespace PDR.BuildingBlocks.Persistence.Behaviors;

/// <summary>
/// Wraps commands (never queries) in a database transaction and translates concurrency conflicts into
/// a deterministic error code instead of a 500.
/// </summary>
public sealed class TransactionBehavior<TRequest, TResponse>(BaseDbContext context)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    public async Task<TResponse> HandleAsync(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken)
    {
        if (!IsCommand(typeof(TRequest)))
        {
            return await next();
        }

        if (context.Database.CurrentTransaction is not null)
        {
            return await next();
        }

        var strategy = context.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync(async () =>
        {
            await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var response = await next();

                if (response is Result { IsFailure: true })
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return response;
                }

                await context.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return response;
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ResultFactory.Failure<TResponse>(Error.Concurrency(
                    "COMMON.CONCURRENCY_CONFLICT",
                    "The record was modified by someone else. Reload and try again."));
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                throw;
            }
        });
    }

    private static bool IsCommand(Type requestType) =>
        requestType.GetInterfaces().Any(i =>
            i == typeof(ICommand) ||
            (i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ICommand<>)));
}
