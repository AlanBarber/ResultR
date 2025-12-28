using MSG = Mediator;

namespace ResultR.Benchmarks.Requests.MediatorSG;

// Simple request/response - just handler, no pipeline
public sealed record MediatorSGSimpleRequest(int Value) : MSG.IRequest<int>;

public sealed class MediatorSGSimpleHandler : MSG.IRequestHandler<MediatorSGSimpleRequest, int>
{
    public ValueTask<int> Handle(MediatorSGSimpleRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(request.Value * 2);
    }
}

// Request with validation pipeline behavior
public sealed record MediatorSGValidatedRequest(int Value) : MSG.IRequest<int>;

public sealed class MediatorSGValidatedHandler : MSG.IRequestHandler<MediatorSGValidatedRequest, int>
{
    public ValueTask<int> Handle(MediatorSGValidatedRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(request.Value * 2);
    }
}

// Validation pipeline behavior for MediatorSG
public sealed class MediatorSGValidationBehavior : MSG.IPipelineBehavior<MediatorSGValidatedRequest, int>
{
    public ValueTask<int> Handle(
        MediatorSGValidatedRequest message,
        MSG.MessageHandlerDelegate<MediatorSGValidatedRequest, int> next,
        CancellationToken cancellationToken)
    {
        // Simulate validation check
        if (message.Value < 0)
        {
            throw new ArgumentException("Value must be non-negative");
        }
        return next(message, cancellationToken);
    }
}

// Request with full pipeline (pre/post behaviors)
public sealed record MediatorSGFullPipelineRequest(int Value) : MSG.IRequest<int>;

public sealed class MediatorSGFullPipelineHandler : MSG.IRequestHandler<MediatorSGFullPipelineRequest, int>
{
    public ValueTask<int> Handle(MediatorSGFullPipelineRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(request.Value * 2);
    }
}

// Pre-processor pipeline behavior for MediatorSG
public sealed class MediatorSGPreProcessorBehavior : MSG.IPipelineBehavior<MediatorSGFullPipelineRequest, int>
{
    public ValueTask<int> Handle(
        MediatorSGFullPipelineRequest message,
        MSG.MessageHandlerDelegate<MediatorSGFullPipelineRequest, int> next,
        CancellationToken cancellationToken)
    {
        // Pre-process
        return next(message, cancellationToken);
    }
}

// Post-processor pipeline behavior for MediatorSG
public sealed class MediatorSGPostProcessorBehavior : MSG.IPipelineBehavior<MediatorSGFullPipelineRequest, int>
{
    public async ValueTask<int> Handle(
        MediatorSGFullPipelineRequest message,
        MSG.MessageHandlerDelegate<MediatorSGFullPipelineRequest, int> next,
        CancellationToken cancellationToken)
    {
        var result = await next(message, cancellationToken);
        // Post-process
        return result;
    }
}
