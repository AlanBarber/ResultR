using MSG = Mediator;

namespace ResultR.Benchmarks.Requests.MediatorSG;

// Simple request/response - just handler, no pipeline
public sealed class MediatorSGSimpleRequest : MSG.IRequest<int> { }

public sealed class MediatorSGSimpleHandler : MSG.IRequestHandler<MediatorSGSimpleRequest, int>
{
    public ValueTask<int> Handle(MediatorSGSimpleRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(84); // 42 * 2
    }
}

// For validated and full pipeline, we just use simple handlers since
// Mediator.SourceGenerator pipelines require IMessage constraint which conflicts
public sealed class MediatorSGValidatedRequest : MSG.IRequest<int> { }

public sealed class MediatorSGValidatedHandler : MSG.IRequestHandler<MediatorSGValidatedRequest, int>
{
    public ValueTask<int> Handle(MediatorSGValidatedRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(84);
    }
}

public sealed class MediatorSGFullPipelineRequest : MSG.IRequest<int> { }

public sealed class MediatorSGFullPipelineHandler : MSG.IRequestHandler<MediatorSGFullPipelineRequest, int>
{
    public ValueTask<int> Handle(MediatorSGFullPipelineRequest request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(84);
    }
}
