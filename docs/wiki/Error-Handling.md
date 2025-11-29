# Error Handling

ResultR provides consistent error handling through the `Result<T>` type. This page covers how errors are handled and best practices for error management.

## Automatic Exception Handling

ResultR automatically catches exceptions in your handlers and converts them to failure results:

```csharp
public async ValueTask<Result<User>> HandleAsync(
    GetUserRequest request, 
    CancellationToken cancellationToken)
{
    // If this throws an exception...
    var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
    return Result<User>.Success(user);
}

// ...ResultR catches it and returns:
// Result<User>.Failure(exception.Message, exception)
```

### What Gets Caught

| Exception Type | Behavior |
|---------------|----------|
| `OperationCanceledException` | **Re-thrown** (allows proper cancellation handling) |
| All other exceptions | **Caught** and converted to `Result.Failure` |

### Accessing the Exception

```csharp
var result = await _dispatcher.Dispatch(request);

if (result.IsFailure)
{
    Console.WriteLine($"Error: {result.Error}");
    
    if (result.Exception is not null)
    {
        // Log the full exception
        _logger.LogError(result.Exception, "Operation failed: {Error}", result.Error);
        
        // Check exception type
        if (result.Exception is SqlException sqlEx)
        {
            // Handle database-specific error
        }
    }
}
```

## Explicit Failure Results

For expected failures (not exceptional conditions), return explicit failure results:

```csharp
public async ValueTask<Result<User>> HandleAsync(
    GetUserRequest request, 
    CancellationToken cancellationToken)
{
    var user = await _repository.GetByIdAsync(request.Id, cancellationToken);
    
    // Expected case: user not found
    if (user is null)
        return Result<User>.Failure($"User {request.Id} not found");
    
    // Expected case: user is inactive
    if (!user.IsActive)
        return Result<User>.Failure("User account is deactivated");
    
    return Result<User>.Success(user);
}
```

## Validation Errors

Use `ValidateAsync` for input validation:

```csharp
public ValueTask<Result> ValidateAsync(CreateUserRequest request)
{
    var errors = new List<string>();
    
    if (string.IsNullOrWhiteSpace(request.Email))
        errors.Add("Email is required");
    else if (!IsValidEmail(request.Email))
        errors.Add("Email format is invalid");
    
    if (string.IsNullOrWhiteSpace(request.Name))
        errors.Add("Name is required");
    else if (request.Name.Length > 100)
        errors.Add("Name cannot exceed 100 characters");
    
    return errors.Count > 0
        ? new(Result.Failure(string.Join("; ", errors)))
        : new(Result.Success());
}
```

## Error Handling Patterns

### In Controllers

```csharp
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    var result = await _dispatcher.Dispatch(new GetUserRequest(id));
    
    if (result.IsSuccess)
        return Ok(result.Value);
    
    // Map errors to appropriate HTTP status codes
    return result.Error switch
    {
        var e when e?.Contains("not found") == true => NotFound(new { error = result.Error }),
        var e when e?.Contains("unauthorized") == true => Unauthorized(new { error = result.Error }),
        _ => BadRequest(new { error = result.Error })
    };
}
```

### Centralized Error Response

Create a helper for consistent error responses:

```csharp
public static class ResultExtensions
{
    public static IActionResult ToActionResult<T>(this Result<T> result)
    {
        if (result.IsSuccess)
            return new OkObjectResult(result.Value);
        
        var problemDetails = new ProblemDetails
        {
            Title = "Operation Failed",
            Detail = result.Error,
            Status = StatusCodes.Status400BadRequest
        };
        
        return new ObjectResult(problemDetails)
        {
            StatusCode = StatusCodes.Status400BadRequest
        };
    }
}

// Usage
[HttpGet("{id}")]
public async Task<IActionResult> GetUser(int id)
{
    var result = await _dispatcher.Dispatch(new GetUserRequest(id));
    return result.ToActionResult();
}
```

### Chaining Operations

Handle errors when chaining multiple operations:

```csharp
public async ValueTask<Result<OrderConfirmation>> HandleAsync(
    PlaceOrderRequest request, 
    CancellationToken cancellationToken)
{
    // Step 1: Validate inventory
    var inventoryResult = await _dispatcher.Dispatch(
        new CheckInventoryRequest(request.Items), cancellationToken);
    
    if (inventoryResult.IsFailure)
        return Result<OrderConfirmation>.Failure($"Inventory check failed: {inventoryResult.Error}");
    
    // Step 2: Create order
    var orderResult = await _dispatcher.Dispatch(
        new CreateOrderRequest(request.CustomerId, request.Items), cancellationToken);
    
    if (orderResult.IsFailure)
        return Result<OrderConfirmation>.Failure($"Order creation failed: {orderResult.Error}");
    
    // Step 3: Process payment
    var paymentResult = await _dispatcher.Dispatch(
        new ProcessPaymentRequest(orderResult.Value.Id, request.Payment), cancellationToken);
    
    if (paymentResult.IsFailure)
    {
        // Compensating action: cancel the order
        await _dispatcher.Dispatch(new CancelOrderRequest(orderResult.Value.Id), cancellationToken);
        return Result<OrderConfirmation>.Failure($"Payment failed: {paymentResult.Error}");
    }
    
    return Result<OrderConfirmation>.Success(
        new OrderConfirmation(orderResult.Value, paymentResult.Value));
}
```

### Using Metadata for Error Context

Add context to errors using metadata:

```csharp
public async ValueTask<Result<Order>> HandleAsync(
    CreateOrderRequest request, 
    CancellationToken cancellationToken)
{
    try
    {
        var order = await _repository.CreateAsync(request, cancellationToken);
        return Result<Order>.Success(order);
    }
    catch (Exception ex)
    {
        return Result<Order>.Failure("Failed to create order", ex)
            .WithMetadata("CustomerId", request.CustomerId)
            .WithMetadata("ItemCount", request.Items.Count)
            .WithMetadata("Timestamp", DateTime.UtcNow);
    }
}
```

## Logging Errors

Use `AfterHandleAsync` for consistent error logging:

```csharp
public ValueTask AfterHandleAsync(MyRequest request, Result<MyResponse> result)
{
    if (result.IsFailure)
    {
        _logger.LogError(
            result.Exception,
            "Request {RequestType} failed: {Error}. Request: {@Request}",
            typeof(MyRequest).Name,
            result.Error,
            request);
    }
    
    return default;
}
```

## Best Practices

1. **Use explicit failures for expected conditions** - Don't throw exceptions for "user not found" or "invalid input"

2. **Reserve exceptions for unexpected conditions** - Database connection failures, network errors, etc.

3. **Include context in error messages** - "User 123 not found" is better than "Not found"

4. **Log exceptions, not just error messages** - The exception contains valuable stack trace information

5. **Don't swallow exceptions silently** - Always log or report them somewhere

6. **Use validation for input errors** - `ValidateAsync` provides a clean separation

## Next Steps

- [Best Practices](Best-Practices) - Recommended patterns and conventions
