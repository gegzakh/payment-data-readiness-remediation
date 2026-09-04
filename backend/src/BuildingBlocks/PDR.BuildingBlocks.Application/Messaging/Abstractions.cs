using PDR.BuildingBlocks.Core.Results;

namespace PDR.BuildingBlocks.Application.Messaging;

public interface IRequest<TResponse>;

/// <summary>State-changing request. Runs inside a transaction and is audited.</summary>
public interface ICommand : IRequest<Result>;

public interface ICommand<TResponse> : IRequest<Result<TResponse>>;

/// <summary>Read-only request. Never opens a write transaction.</summary>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>;

public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}

/// <summary>Chain-of-responsibility step applied to every request (validation, logging, transaction, audit).</summary>
public interface IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    Task<TResponse> HandleAsync(
        TRequest request,
        Func<Task<TResponse>> next,
        CancellationToken cancellationToken);
}

public interface ISender
{
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}

/// <summary>Marks a command whose repeated submission with the same idempotency key must not repeat the effect.</summary>
public interface IIdempotentCommand
{
    string? IdempotencyKey { get; }
}
