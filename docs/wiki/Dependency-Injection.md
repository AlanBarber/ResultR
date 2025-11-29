# Dependency Injection

ResultR integrates seamlessly with Microsoft's dependency injection container. This page covers registration options and best practices.

## Basic Registration

### Auto-Scan Entry Assembly

The simplest setup scans your entry assembly for handlers:

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddResultR();
```

This registers:
- `IDispatcher` as a **scoped** service
- All `IRequestHandler<,>` implementations as **scoped** services

### Specify Assemblies

For multi-project solutions, specify which assemblies to scan:

```csharp
builder.Services.AddResultR(
    typeof(Program).Assembly,           // Web project
    typeof(CreateUserHandler).Assembly, // Application layer
    typeof(SomeOtherHandler).Assembly   // Another assembly
);
```

## Service Lifetimes

### Dispatcher

The `Dispatcher` is registered as **scoped**:

```csharp
services.AddScoped<IDispatcher, Dispatcher>();
```

This means one dispatcher instance per request scope (e.g., per HTTP request in ASP.NET Core).

### Handlers

Handlers are registered as **scoped**:

```csharp
services.AddScoped(handlerInterface, handlerImplementation);
```

This ensures:
- Fresh handler instance per request scope
- Safe to inject scoped dependencies (DbContext, etc.)
- No shared state between requests

## Injecting Dependencies

Handlers support constructor injection:

```csharp
public class CreateUserHandler : IRequestHandler<CreateUserRequest, User>
{
    private readonly IUserRepository _repository;
    private readonly IEmailService _emailService;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(
        IUserRepository repository,
        IEmailService emailService,
        ILogger<CreateUserHandler> logger)
    {
        _repository = repository;
        _emailService = emailService;
        _logger = logger;
    }

    public async ValueTask<Result<User>> HandleAsync(
        CreateUserRequest request, 
        CancellationToken cancellationToken)
    {
        var user = new User(request.Email, request.Name);
        await _repository.AddAsync(user, cancellationToken);
        await _emailService.SendWelcomeEmailAsync(user.Email, cancellationToken);
        
        _logger.LogInformation("User {UserId} created", user.Id);
        
        return Result<User>.Success(user);
    }
}
```

## Using the Dispatcher

### In Controllers

```csharp
[ApiController]
[Route("api/[controller]")]
public class UsersController : ControllerBase
{
    private readonly IDispatcher _dispatcher;

    public UsersController(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost]
    public async Task<IActionResult> Create(CreateUserRequest request)
    {
        var result = await _dispatcher.Dispatch(request);
        
        return result.IsSuccess
            ? CreatedAtAction(nameof(Get), new { id = result.Value.Id }, result.Value)
            : BadRequest(new { error = result.Error });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var result = await _dispatcher.Dispatch(new GetUserRequest(id));
        
        return result.IsSuccess
            ? Ok(result.Value)
            : NotFound(new { error = result.Error });
    }
}
```

### In Services

```csharp
public class OrderService
{
    private readonly IDispatcher _dispatcher;

    public OrderService(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async Task<Order?> CreateOrderAsync(int customerId, List<OrderItem> items)
    {
        var result = await _dispatcher.Dispatch(
            new CreateOrderRequest(customerId, items));
        
        return result.IsSuccess ? result.Value : null;
    }
}
```

### In Handlers (Composition)

Handlers can dispatch to other handlers:

```csharp
public class ProcessCheckoutHandler : IRequestHandler<ProcessCheckoutRequest, CheckoutResult>
{
    private readonly IDispatcher _dispatcher;

    public ProcessCheckoutHandler(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async ValueTask<Result<CheckoutResult>> HandleAsync(
        ProcessCheckoutRequest request, 
        CancellationToken cancellationToken)
    {
        // Create order
        var orderResult = await _dispatcher.Dispatch(
            new CreateOrderRequest(request.CustomerId, request.Items), 
            cancellationToken);
        
        if (orderResult.IsFailure)
            return Result<CheckoutResult>.Failure(orderResult.Error!);

        // Process payment
        var paymentResult = await _dispatcher.Dispatch(
            new ProcessPaymentRequest(orderResult.Value.Id, request.PaymentDetails), 
            cancellationToken);
        
        if (paymentResult.IsFailure)
            return Result<CheckoutResult>.Failure(paymentResult.Error!);

        return Result<CheckoutResult>.Success(
            new CheckoutResult(orderResult.Value, paymentResult.Value));
    }
}
```

## Manual Registration

If you need custom registration (e.g., different lifetime):

```csharp
// Register dispatcher
services.AddScoped<IDispatcher, Dispatcher>();

// Register handlers manually
services.AddTransient<IRequestHandler<MyRequest, MyResponse>, MyHandler>();
services.AddSingleton<IRequestHandler<CachedRequest, CachedResponse>, CachedHandler>();
```

> **Note:** Be careful with singleton handlers - they cannot safely use scoped dependencies.

## Testing

For unit testing, you can mock `IDispatcher`:

```csharp
[Fact]
public async Task Controller_ReturnsOk_WhenDispatcherSucceeds()
{
    // Arrange
    var mockDispatcher = new Mock<IDispatcher>();
    mockDispatcher
        .Setup(d => d.Dispatch(It.IsAny<GetUserRequest>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(Result<User>.Success(new User("test@example.com", "Test")));

    var controller = new UsersController(mockDispatcher.Object);

    // Act
    var result = await controller.Get(1);

    // Assert
    Assert.IsType<OkObjectResult>(result);
}
```

For integration testing, use the real dispatcher with test services:

```csharp
[Fact]
public async Task CreateUser_WithValidData_ReturnsSuccess()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddResultR(typeof(CreateUserHandler).Assembly);
    services.AddScoped<IUserRepository, InMemoryUserRepository>();
    
    var provider = services.BuildServiceProvider();
    var dispatcher = provider.GetRequiredService<IDispatcher>();

    // Act
    var result = await dispatcher.Dispatch(
        new CreateUserRequest("test@example.com", "Test User"));

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("test@example.com", result.Value.Email);
}
```

## Next Steps

- [Error Handling](Error-Handling) - Exception handling patterns
- [Best Practices](Best-Practices) - Recommended patterns
