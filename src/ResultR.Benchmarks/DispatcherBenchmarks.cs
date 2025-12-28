using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using DispatchR.Extensions;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using ResultR.Benchmarks.Requests;
using ResultR.Benchmarks.Requests.DispatchR;
using ResultR.Benchmarks.Requests.MediatorSG;

namespace ResultR.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class DispatcherBenchmarks
{
    private IServiceProvider _resultRProvider = null!;
    private IServiceProvider _mediatRProvider = null!;
    private IServiceProvider _dispatchRProvider = null!;
    private IServiceProvider _mediatorSGProvider = null!;

    private ResultR.IDispatcher _resultRDispatcher = null!;
    private MediatR.IMediator _mediatRMediator = null!;
    private global::DispatchR.IMediator _dispatchRMediator = null!;
    private global::Mediator.IMediator _mediatorSGMediator = null!;

    // Static request instances for Mediator.SourceGenerator (avoids allocation in benchmark)
    private static readonly MediatorSGSimpleRequest _sgSimpleRequest = new(42);
    private static readonly MediatorSGValidatedRequest _sgValidatedRequest = new(42);
    private static readonly MediatorSGFullPipelineRequest _sgFullPipelineRequest = new(42);

    [GlobalSetup]
    public void Setup()
    {
        // ResultR setup
        var resultRServices = new ServiceCollection();
        resultRServices.AddResultR(typeof(DispatcherBenchmarks).Assembly);
        _resultRProvider = resultRServices.BuildServiceProvider();
        _resultRDispatcher = _resultRProvider.GetRequiredService<ResultR.IDispatcher>();

        // MediatR setup - register behaviors for validation and full pipeline
        var mediatRServices = new ServiceCollection();
        mediatRServices.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(DispatcherBenchmarks).Assembly);
            // Register pipeline behaviors for fair comparison
            cfg.AddBehavior<MediatR.IPipelineBehavior<MediatRValidatedRequest, int>, MediatRValidationBehavior<MediatRValidatedRequest, int>>();
            cfg.AddBehavior<MediatR.IPipelineBehavior<MediatRFullPipelineRequest, int>, MediatRPreProcessorBehavior<MediatRFullPipelineRequest, int>>();
            cfg.AddBehavior<MediatR.IPipelineBehavior<MediatRFullPipelineRequest, int>, MediatRPostProcessorBehavior<MediatRFullPipelineRequest, int>>();
        });
        _mediatRProvider = mediatRServices.BuildServiceProvider();
        _mediatRMediator = _mediatRProvider.GetRequiredService<MediatR.IMediator>();

        // DispatchR setup - pipeline behaviors are auto-registered from assembly scan
        var dispatchRServices = new ServiceCollection();
        dispatchRServices.AddDispatchR(typeof(DispatcherBenchmarks).Assembly);
        _dispatchRProvider = dispatchRServices.BuildServiceProvider();
        _dispatchRMediator = _dispatchRProvider.GetRequiredService<global::DispatchR.IMediator>();

        // Mediator.SourceGenerator setup - v3.x provides AddMediator extension
        var mediatorSGServices = new ServiceCollection();
        mediatorSGServices.AddMediator(options =>
        {
            options.ServiceLifetime = ServiceLifetime.Singleton;
        });
        // Register pipeline behaviors manually
        mediatorSGServices.AddSingleton<global::Mediator.IPipelineBehavior<MediatorSGValidatedRequest, int>, MediatorSGValidationBehavior>();
        mediatorSGServices.AddSingleton<global::Mediator.IPipelineBehavior<MediatorSGFullPipelineRequest, int>, MediatorSGPreProcessorBehavior>();
        mediatorSGServices.AddSingleton<global::Mediator.IPipelineBehavior<MediatorSGFullPipelineRequest, int>, MediatorSGPostProcessorBehavior>();
        _mediatorSGProvider = mediatorSGServices.BuildServiceProvider();
        _mediatorSGMediator = _mediatorSGProvider.GetRequiredService<global::Mediator.IMediator>();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        (_resultRProvider as IDisposable)?.Dispose();
        (_mediatRProvider as IDisposable)?.Dispose();
        (_dispatchRProvider as IDisposable)?.Dispose();
        (_mediatorSGProvider as IDisposable)?.Dispose();
    }

    #region Simple Request Benchmarks

    [Benchmark(Description = "ResultR - Simple")]
    public async Task<int> ResultR_Simple()
    {
        var result = await _resultRDispatcher.Dispatch(new ResultRSimpleRequest(42));
        return result.Value;
    }

    [Benchmark(Description = "MediatR - Simple", Baseline = true)]
    public async Task<int> MediatR_Simple()
    {
        return await _mediatRMediator.Send(new MediatRSimpleRequest(42));
    }

    [Benchmark(Description = "DispatchR - Simple")]
    public async Task<int> DispatchR_Simple()
    {
        return await _dispatchRMediator.Send(new DispatchRSimpleRequest(42), CancellationToken.None);
    }

    [Benchmark(Description = "MediatorSG - Simple")]
    public async Task<int> MediatorSG_Simple()
    {
        return await _mediatorSGMediator.Send(_sgSimpleRequest, CancellationToken.None);
    }

    #endregion

    #region Validated Request Benchmarks

    [Benchmark(Description = "ResultR - With Validation")]
    public async Task<int> ResultR_Validated()
    {
        var result = await _resultRDispatcher.Dispatch(new ResultRValidatedRequest(42));
        return result.Value;
    }

    [Benchmark(Description = "MediatR - With Validation")]
    public async Task<int> MediatR_Validated()
    {
        return await _mediatRMediator.Send(new MediatRValidatedRequest(42));
    }

    [Benchmark(Description = "DispatchR - With Validation")]
    public async Task<int> DispatchR_Validated()
    {
        return await _dispatchRMediator.Send(new DispatchRValidatedRequest(42), CancellationToken.None);
    }

    [Benchmark(Description = "MediatorSG - With Validation")]
    public async Task<int> MediatorSG_Validated()
    {
        return await _mediatorSGMediator.Send(_sgValidatedRequest, CancellationToken.None);
    }

    #endregion

    #region Full Pipeline Benchmarks

    [Benchmark(Description = "ResultR - Full Pipeline")]
    public async Task<int> ResultR_FullPipeline()
    {
        var result = await _resultRDispatcher.Dispatch(new ResultRFullPipelineRequest(42));
        return result.Value;
    }

    [Benchmark(Description = "MediatR - Full Pipeline")]
    public async Task<int> MediatR_FullPipeline()
    {
        return await _mediatRMediator.Send(new MediatRFullPipelineRequest(42));
    }

    [Benchmark(Description = "DispatchR - Full Pipeline")]
    public async Task<int> DispatchR_FullPipeline()
    {
        return await _dispatchRMediator.Send(new DispatchRFullPipelineRequest(42), CancellationToken.None);
    }

    [Benchmark(Description = "MediatorSG - Full Pipeline")]
    public async Task<int> MediatorSG_FullPipeline()
    {
        return await _mediatorSGMediator.Send(_sgFullPipelineRequest, CancellationToken.None);
    }

    #endregion
}
