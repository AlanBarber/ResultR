# Best Practices

This page covers recommended patterns and conventions for using ResultR effectively.

## Request Design

### Use Records for Requests

Records provide immutability and value equality:

```csharp
// Good: Immutable record
public record CreateUserRequest(string Email, string Name) : IRequest<User>;

// Avoid: Mutable class
public class CreateUserRequest : IRequest<User>
{
    public string Email { get; set; }  // Mutable
    public string Name { get; set; }
}
```

### Name Requests by Intent

Use clear, action-oriented names:

```csharp
// Good: Clear intent
public record CreateUserRequest(...) : IRequest<User>;
public record GetUserByIdRequest(...) : IRequest<User>;
public record DeactivateUserRequest(...) : IRequest<Result>;

// Avoid: Vague names
public record UserRequest(...) : IRequest<User>;
public record UserData(...) : IRequest<User>;
```

### Keep Requests Focused

Each request should represent a single operation:

```csharp
// Good: Single responsibility
public record CreateUserRequest(string Email, string Name) : IRequest<User>;
public record UpdateUserEmailRequest(int UserId, string NewEmail) : IRequest<User>;

// Avoid: Multiple operations
public record UserRequest(
    string? Email, 
    string? Name, 
    bool IsCreate,      // Flag-driven behavior
    bool IsUpdate) : IRequest<User>;
```

## Handler Design

### One Handler Per Request

Maintain a 1:1 relationship:

```csharp
// Good: Dedicated handler
public class CreateUserHandler : IRequestHandler<CreateUserRequest, User> { }
public class GetUserHandler : IRequestHandler<GetUserRequest, User> { }

// Avoid: Multi-purpose handler (not possible in ResultR anyway)
```

### Keep Handlers Thin

Handlers should orchestrate, not contain all logic:

```csharp
// Good: Delegates to services
public class CreateUserHandler : IRequestHandler<CreateUserRequest, User>
{
    private readonly IUserService _userService;
    
    public async ValueTask<Result<User>> HandleAsync(
        CreateUserRequest request, 
        CancellationToken cancellationToken)
    {
        return await _userService.CreateUserAsync(
            request.Email, 
            request.Name, 
            cancellationToken);
    }
}

// Avoid: All logic in handler
public class CreateUserHandler : IRequestHandler<CreateUserRequest, User>
{
    public async ValueTask<Result<User>> HandleAsync(...)
    {
        // 200 lines of business logic, validation, database calls...
    }
}
```

### Use Constructor Injection

Inject dependencies through the constructor:

```csharp
// Good: Constructor injection
public class CreateUserHandler : IRequestHandler<CreateUserRequest, User>
{
    private readonly IUserRepository _repository;
    private readonly ILogger<CreateUserHandler> _logger;

    public CreateUserHandler(IUserRepository repository, ILogger<CreateUserHandler> logger)
    {
        _repository = repository;
        _logger = logger;
    }
}
```

## Validation

### Validate Early

Use `ValidateAsync` for input validation:

```csharp
public ValueTask<Result> ValidateAsync(CreateUserRequest request)
{
    if (string.IsNullOrWhiteSpace(request.Email))
        return new(Result.Failure("Email is required"));
    
    if (!request.Email.Contains('@'))
        return new(Result.Failure("Invalid email format"));
    
    return new(Result.Success());
}
```

### Separate Input vs Business Validation

- **Input validation** (in `ValidateAsync`): Format, required fields, length limits
- **Business validation** (in `HandleAsync`): Uniqueness, permissions, business rules

```csharp
public ValueTask<Result> ValidateAsync(CreateUserRequest request)
{
    // Input validation only
    if (string.IsNullOrWhiteSpace(request.Email))
        return new(Result.Failure("Email is required"));
    
    return new(Result.Success());
}

public async ValueTask<Result<User>> HandleAsync(
    CreateUserRequest request, 
    CancellationToken cancellationToken)
{
    // Business validation
    if (await _repository.EmailExistsAsync(request.Email, cancellationToken))
        return Result<User>.Failure("Email already registered");
    
    // Create user...
}
```

## Result Handling

### Check Results Immediately

Don't ignore results:

```csharp
// Good: Check result
var result = await _dispatcher.Dispatch(request);
if (result.IsFailure)
    return Result<Order>.Failure(result.Error!);

// Avoid: Ignoring result
await _dispatcher.Dispatch(request);  // What if it failed?
```

### Use Explicit Failures for Expected Cases

```csharp
// Good: Explicit failure for expected case
var user = await _repository.GetByIdAsync(id, ct);
if (user is null)
    return Result<User>.Failure($"User {id} not found");

// Avoid: Throwing for expected case
var user = await _repository.GetByIdAsync(id, ct) 
    ?? throw new NotFoundException($"User {id} not found");
```

### Include Context in Error Messages

```csharp
// Good: Contextual error
return Result<User>.Failure($"User {request.Id} not found");
return Result<Order>.Failure($"Cannot cancel order {orderId}: already shipped");

// Avoid: Generic error
return Result<User>.Failure("Not found");
return Result<Order>.Failure("Invalid operation");
```

## Project Organization

### Organize by Feature

```
src/
├── MyApp.Application/
│   ├── Users/
│   │   ├── CreateUser/
│   │   │   ├── CreateUserRequest.cs
│   │   │   └── CreateUserHandler.cs
│   │   ├── GetUser/
│   │   │   ├── GetUserRequest.cs
│   │   │   └── GetUserHandler.cs
│   │   └── UpdateUser/
│   │       ├── UpdateUserRequest.cs
│   │       └── UpdateUserHandler.cs
│   └── Orders/
│       └── ...
```

### Or Organize by Type

```
src/
├── MyApp.Application/
│   ├── Requests/
│   │   ├── CreateUserRequest.cs
│   │   ├── GetUserRequest.cs
│   │   └── CreateOrderRequest.cs
│   └── Handlers/
│       ├── CreateUserHandler.cs
│       ├── GetUserHandler.cs
│       └── CreateOrderHandler.cs
```

## Testing

### Test Handlers Directly

```csharp
[Fact]
public async Task CreateUser_WithValidData_ReturnsSuccess()
{
    // Arrange
    var repository = new InMemoryUserRepository();
    var handler = new CreateUserHandler(repository);
    var request = new CreateUserRequest("test@example.com", "Test User");

    // Act
    var result = await handler.HandleAsync(request, CancellationToken.None);

    // Assert
    Assert.True(result.IsSuccess);
    Assert.Equal("test@example.com", result.Value.Email);
}
```

### Test Validation Separately

```csharp
[Theory]
[InlineData("", "Email is required")]
[InlineData("invalid", "Invalid email format")]
public async Task Validate_WithInvalidEmail_ReturnsFailure(string email, string expectedError)
{
    // Arrange
    var handler = new CreateUserHandler(Mock.Of<IUserRepository>());
    var request = new CreateUserRequest(email, "Test");

    // Act
    var result = await handler.ValidateAsync(request);

    // Assert
    Assert.True(result.IsFailure);
    Assert.Contains(expectedError, result.Error);
}
```

### Test Through Dispatcher for Integration

```csharp
[Fact]
public async Task CreateUser_Integration_WorksEndToEnd()
{
    // Arrange
    var services = new ServiceCollection();
    services.AddResultR(typeof(CreateUserHandler).Assembly);
    services.AddScoped<IUserRepository, InMemoryUserRepository>();
    
    var provider = services.BuildServiceProvider();
    var dispatcher = provider.GetRequiredService<IDispatcher>();

    // Act
    var result = await dispatcher.Dispatch(
        new CreateUserRequest("test@example.com", "Test"));

    // Assert
    Assert.True(result.IsSuccess);
}
```

## Performance

### Avoid Unnecessary Async

If your operation is synchronous, wrap it efficiently:

```csharp
// Good: Synchronous operation wrapped efficiently
public ValueTask<Result<int>> HandleAsync(
    CalculateRequest request, 
    CancellationToken cancellationToken)
{
    var result = request.A + request.B;
    return new(Result<int>.Success(result));
}

// Avoid: Unnecessary async
public async ValueTask<Result<int>> HandleAsync(
    CalculateRequest request, 
    CancellationToken cancellationToken)
{
    await Task.CompletedTask;  // Unnecessary
    var result = request.A + request.B;
    return Result<int>.Success(result);
}
```

### Pass CancellationToken Through

Always pass the cancellation token to async operations:

```csharp
public async ValueTask<Result<User>> HandleAsync(
    GetUserRequest request, 
    CancellationToken cancellationToken)
{
    // Good: Pass token
    var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
    
    // Avoid: Ignoring token
    var user = await _repository.GetByIdAsync(request.Id);
}
```

## Summary

| Do | Don't |
|----|-------|
| Use records for requests | Use mutable classes |
| Keep handlers thin | Put all logic in handlers |
| Validate early with `ValidateAsync` | Throw exceptions for validation |
| Check results immediately | Ignore results |
| Include context in errors | Use generic error messages |
| Pass `CancellationToken` through | Ignore cancellation |
| Test handlers directly | Only test through HTTP |
