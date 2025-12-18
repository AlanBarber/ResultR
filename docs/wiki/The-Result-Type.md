# The Result Type

ResultR uses the `Result<T>` type to represent the outcome of operations. This eliminates the need for exceptions in normal control flow and makes success/failure handling explicit.

## Overview

Every handler returns `Result<T>` where `T` is your response type:

```csharp
public ValueTask<Result<User>> HandleAsync(GetUserRequest request, CancellationToken ct)
{
    // Return success or failure
}
```

## Creating Results

### Success

```csharp
// With a value
var result = Result<User>.Success(user);

// Implicit conversion (shorthand)
Result<User> result = user;
```

### Failure

```csharp
// With an error message
var result = Result<User>.Failure("User not found");

// With an error message and exception
var result = Result<User>.Failure("Database error", exception);
```

## Checking Results

```csharp
var result = await _dispatcher.Dispatch(request);

if (result.IsSuccess)
{
    var user = result.Value;  // Safe to access
    Console.WriteLine($"Found: {user.Name}");
}
else
{
    Console.WriteLine($"Error: {result.Error}");
    
    // Exception is available if one was captured
    if (result.Exception is not null)
    {
        _logger.LogError(result.Exception, "Operation failed");
    }
}
```

## Properties

| Property | Type | Description |
|----------|------|-------------|
| `IsSuccess` | `bool` | `true` if the operation succeeded |
| `IsFailure` | `bool` | `true` if the operation failed |
| `Value` | `T` | The result value (throws if `IsFailure`) |
| `Error` | `string?` | Error message (null if `IsSuccess`) |
| `Exception` | `Exception?` | Captured exception (null if none) |
| `Metadata` | `IReadOnlyDictionary<string, object>` | Optional metadata |

## The Non-Generic Result

For operations that don't return a value, use `Result`:

```csharp
// Creating
var success = Result.Success();
var failure = Result.Failure("Something went wrong");

// Checking
if (result.IsSuccess)
{
    Console.WriteLine("Operation completed");
}
```

## Metadata

Attach additional context to results:

```csharp
var result = Result<User>.Success(user)
    .WithMetadata("CreatedAt", DateTime.UtcNow)
    .WithMetadata("Source", "API")
    .WithMetadata("RequestId", Guid.NewGuid());

// Access metadata with type-safe helper
var createdAt = result.GetMetadataValueOrDefault<DateTime>("CreatedAt");
var source = result.GetMetadataValueOrDefault<string>("Source");

// Returns default if key doesn't exist or type doesn't match
var missing = result.GetMetadataValueOrDefault<int>("NonExistent"); // Returns 0
var wrongType = result.GetMetadataValueOrDefault<string>("CreatedAt"); // Returns null

// Direct dictionary access is also available
var requestId = (Guid)result.Metadata["RequestId"];
```

## Patterns

### Early Return on Failure

```csharp
public async ValueTask<Result<OrderSummary>> HandleAsync(
    GetOrderSummaryRequest request, 
    CancellationToken ct)
{
    var userResult = await _dispatcher.Dispatch(new GetUserRequest(request.UserId), ct);
    if (userResult.IsFailure)
        return Result<OrderSummary>.Failure(userResult.Error!);

    var ordersResult = await _dispatcher.Dispatch(new GetOrdersRequest(request.UserId), ct);
    if (ordersResult.IsFailure)
        return Result<OrderSummary>.Failure(ordersResult.Error!);

    return Result<OrderSummary>.Success(
        new OrderSummary(userResult.Value, ordersResult.Value));
}
```

### Conditional Success/Failure

```csharp
public async ValueTask<Result<User>> HandleAsync(
    GetUserRequest request, 
    CancellationToken ct)
{
    var user = await _repository.GetByIdAsync(request.Id, ct);
    
    return user is not null
        ? Result<User>.Success(user)
        : Result<User>.Failure($"User {request.Id} not found");
}
```

### Transforming Results in Controllers

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    var result = await _dispatcher.Dispatch(new GetUserRequest(id));
    
    return result.IsSuccess
        ? Ok(result.Value)
        : NotFound(new { error = result.Error });
}

[HttpPost]
public async Task<IActionResult> CreateUser(CreateUserRequest request)
{
    var result = await _dispatcher.Dispatch(request);
    
    return result.IsSuccess
        ? CreatedAtAction(nameof(GetUser), new { id = result.Value.Id }, result.Value)
        : BadRequest(new { error = result.Error });
}
```

## Exception Handling

ResultR automatically catches exceptions in handlers and converts them to failure results:

```csharp
public async ValueTask<Result<User>> HandleAsync(
    GetUserRequest request, 
    CancellationToken ct)
{
    // If this throws, ResultR catches it and returns:
    // Result<User>.Failure(exception.Message, exception)
    var user = await _repository.GetByIdAsync(request.Id, ct);
    return Result<User>.Success(user);
}
```

The only exception that is **not** caught is `OperationCanceledException`, which is re-thrown to allow proper cancellation handling.

## Next Steps

- [Pipeline Hooks](Pipeline-Hooks) - Add validation and pre/post processing
- [Error Handling](Error-Handling) - Advanced error handling patterns
