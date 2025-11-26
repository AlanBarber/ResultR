using MediatR;

namespace ResultR.Benchmarks.Requests;

// Simple request/response
public record MediatRSimpleRequest(int Value) : MediatR.IRequest<int>;

public class MediatRSimpleHandler : MediatR.IRequestHandler<MediatRSimpleRequest, int>
{
    public Task<int> Handle(MediatRSimpleRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Value * 2);
    }
}

// Request with pipeline behavior (validation equivalent)
public record MediatRValidatedRequest(int Value) : MediatR.IRequest<int>;

public class MediatRValidatedHandler : MediatR.IRequestHandler<MediatRValidatedRequest, int>
{
    public Task<int> Handle(MediatRValidatedRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Value * 2);
    }
}

public class MediatRValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Simulate validation
        if (request is MediatRValidatedRequest validated && validated.Value < 0)
        {
            throw new ArgumentException("Value must be non-negative");
        }

        return await next();
    }
}

// Request with full pipeline (pre/post behaviors)
public record MediatRFullPipelineRequest(int Value) : MediatR.IRequest<int>;

public class MediatRFullPipelineHandler : MediatR.IRequestHandler<MediatRFullPipelineRequest, int>
{
    public Task<int> Handle(MediatRFullPipelineRequest request, CancellationToken cancellationToken)
    {
        return Task.FromResult(request.Value * 2);
    }
}

public class MediatRPreProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        // Pre-process
        return await next();
    }
}

public class MediatRPostProcessorBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        var response = await next();
        // Post-process
        return response;
    }
}
