# Pipeline Hooks

ResultR provides optional lifecycle hooks that run before and after your handler. These are defined as virtual methods on `IRequestHandler<TRequest, TResponse>` with default implementations, so you only override what you need.

## Pipeline Order

```
1. ValidateAsync      → Return failure to short-circuit
2. BeforeHandleAsync  → Pre-processing (logging, setup)
3. HandleAsync        → Your business logic (required)
4. AfterHandleAsync   → Post-processing (logging, cleanup)
```

## ValidateAsync

Validates the request before processing. Return `Result.Failure()` to short-circuit the pipeline.

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserRequest, User>
{
    public ValueTask<Result> ValidateAsync(CreateUserRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return new(Result.Failure("Email is required"));
        
        if (!request.Email.Contains('@'))
            return new(Result.Failure("Invalid email format"));
        
        if (string.IsNullOrWhiteSpace(request.Name))
            return new(Result.Failure("Name is required"));
        
        return new(Result.Success());
    }

    public async ValueTask<Result<User>> HandleAsync(
        CreateUserRequest request, 
        CancellationToken cancellationToken)
    {
        // Only runs if validation passes
        var user = new User(request.Email, request.Name);
        await _repository.AddAsync(user, cancellationToken);
        return Result<User>.Success(user);
    }
}
```

### Validation with Exceptions

You can include an exception in validation failures:

```csharp
public ValueTask<Result> ValidateAsync(ProcessPaymentRequest request)
{
    try
    {
        // Complex validation that might throw
        ValidateCreditCard(request.CardNumber);
        return new(Result.Success());
    }
    catch (ValidationException ex)
    {
        return new(Result.Failure(ex.Message, ex));
    }
}
```

## BeforeHandleAsync

Runs after validation passes, before the main handler. Use for setup, logging, or cross-cutting concerns.

```csharp
public class AuditedHandler : IRequestHandler<SensitiveRequest, SensitiveData>
{
    private readonly ILogger<AuditedHandler> _logger;
    private readonly IAuditService _audit;

    public ValueTask BeforeHandleAsync(SensitiveRequest request)
    {
        _logger.LogInformation(
            "Processing sensitive request for user {UserId}", 
            request.UserId);
        
        _audit.LogAccess(request.UserId, "SensitiveData");
        
        return default;
    }

    public async ValueTask<Result<SensitiveData>> HandleAsync(
        SensitiveRequest request, 
        CancellationToken cancellationToken)
    {
        // Main logic
    }
}
```

## AfterHandleAsync

Runs after the handler completes, regardless of success or failure. Receives the result for inspection.

```csharp
public class MetricsHandler : IRequestHandler<OrderRequest, Order>
{
    private readonly IMetricsService _metrics;
    private readonly ILogger<MetricsHandler> _logger;
    private Stopwatch? _stopwatch;

    public ValueTask BeforeHandleAsync(OrderRequest request)
    {
        _stopwatch = Stopwatch.StartNew();
        return default;
    }

    public async ValueTask<Result<Order>> HandleAsync(
        OrderRequest request, 
        CancellationToken cancellationToken)
    {
        // Main logic
    }

    public ValueTask AfterHandleAsync(OrderRequest request, Result<Order> result)
    {
        _stopwatch?.Stop();
        
        _metrics.RecordDuration("order_processing", _stopwatch?.ElapsedMilliseconds ?? 0);
        
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Order {OrderId} created in {Duration}ms", 
                result.Value.Id, 
                _stopwatch?.ElapsedMilliseconds);
        }
        else
        {
            _logger.LogWarning(
                "Order creation failed: {Error}", 
                result.Error);
            _metrics.IncrementCounter("order_failures");
        }
        
        return default;
    }
}
```

## Complete Example

A handler using all hooks:

```csharp
public class CreateOrderHandler : IRequestHandler<CreateOrderRequest, Order>
{
    private readonly IOrderRepository _repository;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        IOrderRepository repository, 
        ILogger<CreateOrderHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    // 1. Validation
    public ValueTask<Result> ValidateAsync(CreateOrderRequest request)
    {
        if (request.Items.Count == 0)
            return new(Result.Failure("Order must have at least one item"));
        
        if (request.Items.Any(i => i.Quantity <= 0))
            return new(Result.Failure("All items must have positive quantity"));
        
        return new(Result.Success());
    }

    // 2. Before handling
    public ValueTask BeforeHandleAsync(CreateOrderRequest request)
    {
        _logger.LogInformation(
            "Creating order with {ItemCount} items for customer {CustomerId}",
            request.Items.Count,
            request.CustomerId);
        
        return default;
    }

    // 3. Main handler
    public async ValueTask<Result<Order>> HandleAsync(
        CreateOrderRequest request, 
        CancellationToken cancellationToken)
    {
        var order = new Order(request.CustomerId, request.Items);
        await _repository.AddAsync(order, cancellationToken);
        return Result<Order>.Success(order);
    }

    // 4. After handling
    public ValueTask AfterHandleAsync(CreateOrderRequest request, Result<Order> result)
    {
        if (result.IsSuccess)
        {
            _logger.LogInformation(
                "Order {OrderId} created successfully", 
                result.Value.Id);
        }
        else
        {
            _logger.LogError(
                "Failed to create order: {Error}", 
                result.Error);
        }
        
        return default;
    }
}
```

## Default Implementations

All hooks have default implementations, so you only override what you need:

```csharp
// Interface defaults (you don't write these)
virtual ValueTask<Result> ValidateAsync(TRequest request) => new(Result.Success());
virtual ValueTask BeforeHandleAsync(TRequest request) => default;
virtual ValueTask AfterHandleAsync(TRequest request, Result<TResponse> result) => default;
```

## Important Notes

1. **Validation failures short-circuit** - If `ValidateAsync` returns failure, `BeforeHandleAsync`, `HandleAsync`, and `AfterHandleAsync` do not run.

2. **AfterHandleAsync always runs** - After `HandleAsync` completes (success or failure), `AfterHandleAsync` is called with the result.

3. **Exceptions are caught** - Any exception in the pipeline (except `OperationCanceledException`) is caught and converted to a failure result.

4. **No base class required** - Just implement the interface and override the methods you need.

## Next Steps

- [Error Handling](Error-Handling) - Advanced error handling patterns
- [Best Practices](Best-Practices) - Recommended patterns
