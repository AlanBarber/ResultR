# Requests and Handlers

This page covers how to create requests and handlers in ResultR.

## Requests

A request represents an action or query in your application. Requests implement `IRequest<TResponse>` when returning a value, or `IRequest` for void operations.

### Basic Request

```csharp
public record GetUserRequest(int Id) : IRequest<User>;
```

### Request with Multiple Parameters

```csharp
public record SearchUsersRequest(
    string? NameFilter,
    int Page,
    int PageSize) : IRequest<PagedResult<User>>;
```

### Request Returning a Collection

```csharp
public record GetAllOrdersRequest(int CustomerId) : IRequest<IReadOnlyList<Order>>;
```

### Request with No Return Value

For operations that don't return data (commands), use the non-generic `IRequest` interface:

```csharp
public record DeleteUserRequest(int Id) : IRequest;
```

## Handlers

Handlers contain the business logic for processing requests. Each request type has exactly one handler.

### Basic Handler

```csharp
public class GetUserHandler : IRequestHandler<GetUserRequest, User>
{
    private readonly IUserRepository _repository;

    public GetUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result<User>> HandleAsync(
        GetUserRequest request, 
        CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
        
        return user is not null
            ? Result<User>.Success(user)
            : Result<User>.Failure($"User {request.Id} not found");
    }
}
```

### Handler with Dependencies

Handlers support constructor injection - any dependencies registered with your DI container can be injected:

```csharp
public class CreateOrderHandler : IRequestHandler<CreateOrderRequest, Order>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryService _inventoryService;
    private readonly ILogger<CreateOrderHandler> _logger;

    public CreateOrderHandler(
        IOrderRepository orderRepository,
        IInventoryService inventoryService,
        ILogger<CreateOrderHandler> logger)
    {
        _orderRepository = orderRepository;
        _inventoryService = inventoryService;
        _logger = logger;
    }

    public async ValueTask<Result<Order>> HandleAsync(
        CreateOrderRequest request, 
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating order for customer {CustomerId}", request.CustomerId);
        
        // Check inventory
        if (!await _inventoryService.CheckAvailabilityAsync(request.Items, cancellationToken))
        {
            return Result<Order>.Failure("One or more items are out of stock");
        }

        // Create order
        var order = new Order(request.CustomerId, request.Items);
        await _orderRepository.AddAsync(order, cancellationToken);
        
        return Result<Order>.Success(order);
    }
}
```

### Handler for Void Operations

When your operation doesn't return a value, use `IRequest` and `IRequestHandler<TRequest>`:

```csharp
public record DeleteUserRequest(int Id) : IRequest;

public class DeleteUserHandler : IRequestHandler<DeleteUserRequest>
{
    private readonly IUserRepository _repository;

    public DeleteUserHandler(IUserRepository repository)
    {
        _repository = repository;
    }

    public async ValueTask<Result> HandleAsync(
        DeleteUserRequest request, 
        CancellationToken cancellationToken)
    {
        var deleted = await _repository.DeleteAsync(request.Id, cancellationToken);
        
        return deleted
            ? Result.Success()
            : Result.Failure($"User {request.Id} not found");
    }
}
```

## Handler Registration

Handlers are automatically discovered and registered when you call `AddResultR()`:

```csharp
// Scans the entry assembly for handlers
services.AddResultR();

// Or specify assemblies explicitly
services.AddResultR(typeof(MyHandler).Assembly);
```

Handlers are registered as **scoped** services, meaning a new instance is created for each request scope (e.g., each HTTP request in ASP.NET Core).

## One Handler Per Request

ResultR enforces a 1:1 relationship between requests and handlers. If you need to:

- **Reuse logic** - Extract shared code into services and inject them into handlers
- **Handle multiple request types** - Create separate handlers for each request type
- **Compose operations** - Have one handler dispatch to other requests

```csharp
// Composing handlers
public class ComplexOperationHandler : IRequestHandler<ComplexRequest, ComplexResult>
{
    private readonly IDispatcher _dispatcher;

    public ComplexOperationHandler(IDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    public async ValueTask<Result<ComplexResult>> HandleAsync(
        ComplexRequest request, 
        CancellationToken cancellationToken)
    {
        // Dispatch to other handlers
        var userResult = await _dispatcher.Dispatch(new GetUserRequest(request.UserId), cancellationToken);
        if (userResult.IsFailure)
            return Result<ComplexResult>.Failure(userResult.Error!);

        var ordersResult = await _dispatcher.Dispatch(new GetOrdersRequest(request.UserId), cancellationToken);
        if (ordersResult.IsFailure)
            return Result<ComplexResult>.Failure(ordersResult.Error!);

        return Result<ComplexResult>.Success(new ComplexResult(userResult.Value, ordersResult.Value));
    }
}
```

## Next Steps

- [The Result Type](The-Result-Type) - Learn about handling success and failure
- [Pipeline Hooks](Pipeline-Hooks) - Add validation and pre/post processing
