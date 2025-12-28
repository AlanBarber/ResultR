# ResultR.Validation

**ResultR.Validation** is an optional companion package that provides a lightweight, inline validation framework designed specifically for ResultR's `ValidateAsync()` pipeline hook.

## Overview

Unlike FluentValidation which requires separate validator classes and DI registration, ResultR.Validation allows you to define validation rules **directly inside your `ValidateAsync()` method** using a fluent API. It integrates seamlessly with ResultR's `Result` type, automatically returning `Result.Success()` or `Result.Failure()` with aggregated validation errors.

## Installation

```bash
dotnet add package ResultR.Validation
```

## Key Benefits

- ✅ **Zero ceremony** - No external validator classes, no DI registration for validators
- ✅ **Inline validation** - Define rules directly in `ValidateAsync()` using a fluent API
- ✅ **Seamless integration** - Works with both `IRequestHandler<TRequest>` and `IRequestHandler<TRequest, TResponse>`
- ✅ **Automatic result conversion** - Returns `Result.Success()` when all validations pass, or `Result.Failure()` with aggregated errors
- ✅ **Comprehensive built-in rules** - String, numeric, collection, and custom validations out of the box

## Quick Start

```csharp
using ResultR;
using ResultR.Validation;

public record CreateUserRequest(string Email, string Name, int Age) : IRequest<User>;

public class CreateUserHandler : IRequestHandler<CreateUserRequest, User>
{
    public ValueTask<Result> ValidateAsync(CreateUserRequest request)
    {
        return Validator.For(request)
            .RuleFor(x => x.Email)
                .NotEmpty("Email is required")
                .EmailAddress("Invalid email format")
            .RuleFor(x => x.Name)
                .NotEmpty("Name is required")
                .MinLength(2, "Name must be at least 2 characters")
                .MaxLength(100, "Name cannot exceed 100 characters")
            .RuleFor(x => x.Age)
                .GreaterThan(0, "Age must be positive")
                .LessThanOrEqualTo(150, "Age must be realistic")
            .ToResult();
    }

    public async ValueTask<Result<User>> HandleAsync(CreateUserRequest request, CancellationToken ct)
    {
        // This only runs if validation passes
        var user = new User(request.Email, request.Name, request.Age);
        await _repository.AddAsync(user, ct);
        return Result<User>.Success(user);
    }
}
```

## Built-in Validation Rules

### String Validations

#### NotEmpty()
Validates that a string is not null, empty, or whitespace.

```csharp
.RuleFor(x => x.Name)
    .NotEmpty("Name is required")
```

#### MinLength(int minLength)
Validates that a string has a minimum length.

```csharp
.RuleFor(x => x.Password)
    .MinLength(8, "Password must be at least 8 characters")
```

#### MaxLength(int maxLength)
Validates that a string does not exceed a maximum length.

```csharp
.RuleFor(x => x.Username)
    .MaxLength(50, "Username cannot exceed 50 characters")
```

#### Length(int minLength, int maxLength)
Validates that a string length is within a specified range.

```csharp
.RuleFor(x => x.ZipCode)
    .Length(5, 10, "Zip code must be between 5 and 10 characters")
```

#### Matches(string pattern, RegexOptions options = RegexOptions.None)
Validates that a string matches a regular expression pattern.

```csharp
.RuleFor(x => x.PhoneNumber)
    .Matches(@"^\d{3}-\d{3}-\d{4}$", "Phone number must be in format: 123-456-7890")
```

#### EmailAddress()
Validates that a string is a valid email address format.

```csharp
.RuleFor(x => x.Email)
    .EmailAddress("Invalid email format")
```

### Numeric Validations

#### GreaterThan(T comparisonValue)
Validates that a value is greater than the specified comparison value.

```csharp
.RuleFor(x => x.Age)
    .GreaterThan(0, "Age must be positive")
```

#### GreaterThanOrEqualTo(T comparisonValue)
Validates that a value is greater than or equal to the specified comparison value.

```csharp
.RuleFor(x => x.Quantity)
    .GreaterThanOrEqualTo(1, "Quantity must be at least 1")
```

#### LessThan(T comparisonValue)
Validates that a value is less than the specified comparison value.

```csharp
.RuleFor(x => x.Discount)
    .LessThan(100, "Discount must be less than 100%")
```

#### LessThanOrEqualTo(T comparisonValue)
Validates that a value is less than or equal to the specified comparison value.

```csharp
.RuleFor(x => x.Age)
    .LessThanOrEqualTo(150, "Age must be realistic")
```

#### Between(T minValue, T maxValue)
Validates that a value is within the specified range (inclusive).

```csharp
.RuleFor(x => x.Rating)
    .Between(1, 5, "Rating must be between 1 and 5")
```

### Collection Validations

#### NotEmpty()
Validates that a collection is not null or empty.

```csharp
.RuleFor(x => x.Items)
    .NotEmpty("Order must contain at least one item")
```

### General Validations

#### NotNull()
Validates that a value is not null.

```csharp
.RuleFor(x => x.Address)
    .NotNull("Address is required")
```

#### Equal(T comparisonValue)
Validates that a value equals the specified comparison value.

```csharp
.RuleFor(x => x.ConfirmPassword)
    .Equal(request.Password, "Passwords must match")
```

#### NotEqual(T disallowedValue)
Validates that a value does not equal the specified disallowed value.

```csharp
.RuleFor(x => x.NewEmail)
    .NotEqual(currentEmail, "New email must be different from current email")
```

#### Must(Func<TProperty, bool> predicate, string message)
Validates that a value satisfies a custom predicate.

```csharp
.RuleFor(x => x.Email)
    .Must(email => email.EndsWith("@company.com"), "Must use company email")
```

## Custom Validation Rules

Use the `Must()` method to implement custom validation logic:

```csharp
public ValueTask<Result> ValidateAsync(CreateUserRequest request)
{
    return Validator.For(request)
        .RuleFor(x => x.Email)
            .NotEmpty("Email is required")
            .Must(email => email.EndsWith("@company.com"), "Must use company email")
            .Must(email => !_blacklist.Contains(email), "Email is blacklisted")
        .RuleFor(x => x.Name)
            .Must(name => !name.Contains("admin", StringComparison.OrdinalIgnoreCase), 
                  "Name cannot contain 'admin'")
            .Must(name => !_profanityFilter.ContainsProfanity(name), 
                  "Name contains inappropriate language")
        .ToResult();
}
```

## Accessing Validation Errors

When validation fails, errors are stored in the `Result` metadata under the `ValidationErrors` key:

```csharp
var result = await _dispatcher.Dispatch(request);

if (result.IsFailure)
{
    var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(
        ValidationMetadataKeys.ValidationErrors);
    
    if (errors is not null)
    {
        foreach (var error in errors)
        {
            Console.WriteLine($"{error.PropertyName}: {error.ErrorMessage}");
        }
    }
}

// Example output:
// Email: Email is required
// Name: Name must be at least 2 characters
// Age: Age must be positive
```

### ValidationError Record

```csharp
public record ValidationError(string PropertyName, string ErrorMessage);
```

## How It Works

1. **`Validator.For(request)`** creates a `ValidationBuilder<T>` for the request
2. **`RuleFor(x => x.Property)`** selects a property and returns a `RuleBuilder<T, TProperty>`
3. **Validation methods** (e.g., `NotEmpty()`, `MinLength()`) add rules to an internal list
4. **`ToResult()`** executes all rules and returns:
   - `Result.Success()` if all rules pass
   - `Result.Failure("Validation failed")` with errors in metadata if any rule fails

## Chaining Multiple Properties

You can validate multiple properties in a single fluent chain:

```csharp
return Validator.For(request)
    .RuleFor(x => x.Email)
        .NotEmpty("Email is required")
        .EmailAddress("Invalid email format")
    .RuleFor(x => x.Name)
        .NotEmpty("Name is required")
        .MinLength(2, "Name too short")
    .RuleFor(x => x.Age)
        .GreaterThan(0, "Age must be positive")
    .ToResult();
```

## Default Error Messages

Most validation rules provide default error messages if you don't specify a custom message:

```csharp
.RuleFor(x => x.Name)
    .NotEmpty()  // Default: "Name is required."
    .MinLength(2)  // Default: "Name must be at least 2 characters."
```

Custom messages override the defaults:

```csharp
.RuleFor(x => x.Name)
    .NotEmpty("Please enter your name")
    .MinLength(2, "Your name is too short")
```

## Integration with ASP.NET Core

### Returning Validation Errors in API Responses

```csharp
[HttpPost]
public async Task<IActionResult> CreateUser(CreateUserRequest request)
{
    var result = await _dispatcher.Dispatch(request);
    
    if (result.IsFailure)
    {
        var validationErrors = result.GetMetadataValueOrDefault<List<ValidationError>>(
            ValidationMetadataKeys.ValidationErrors);
        
        if (validationErrors is not null)
        {
            var errors = validationErrors.ToDictionary(
                e => e.PropertyName, 
                e => e.ErrorMessage);
            
            return BadRequest(new { errors });
        }
        
        return BadRequest(new { error = result.Error });
    }
    
    return Ok(result.Value);
}
```

### ModelState Integration

```csharp
if (result.IsFailure)
{
    var validationErrors = result.GetMetadataValueOrDefault<List<ValidationError>>(
        ValidationMetadataKeys.ValidationErrors);
    
    if (validationErrors is not null)
    {
        foreach (var error in validationErrors)
        {
            ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
        }
        return ValidationProblem(ModelState);
    }
}
```

## Comparison with FluentValidation

| Feature | ResultR.Validation | FluentValidation |
|---------|-------------------|------------------|
| Validator classes | ❌ Not required | ✅ Required |
| DI registration | ❌ Not required | ✅ Required |
| Inline validation | ✅ Yes | ❌ No |
| Location | Inside handler | Separate class |
| Setup complexity | Minimal | Higher |
| Flexibility | Good for simple cases | Excellent for complex scenarios |
| Reusability | Per-handler | Across application |

**When to use ResultR.Validation:**
- Simple to moderate validation requirements
- You prefer keeping validation close to handler logic
- You want minimal setup and configuration
- You're already using ResultR

**When to use FluentValidation:**
- Complex validation scenarios with many rules
- You need to reuse validators across multiple handlers
- You require advanced features (custom validators, rule sets, etc.)
- You need validation outside of the request/handler pattern

## Best Practices

### 1. Keep Validation Rules Simple

```csharp
// Good: Simple, readable rules
.RuleFor(x => x.Email)
    .NotEmpty("Email is required")
    .EmailAddress("Invalid email format")

// Avoid: Complex logic in Must()
.RuleFor(x => x.Email)
    .Must(email => {
        var domain = email.Split('@')[1];
        return _allowedDomains.Contains(domain) && 
               !_blacklist.Contains(email) &&
               email.Length < 100;
    }, "Email validation failed")
```

### 2. Use Descriptive Error Messages

```csharp
// Good: Clear, actionable message
.RuleFor(x => x.Password)
    .MinLength(8, "Password must be at least 8 characters long")

// Avoid: Vague message
.RuleFor(x => x.Password)
    .MinLength(8, "Invalid password")
```

### 3. Validate Business Rules in HandleAsync

```csharp
// ValidateAsync: Input validation only
public ValueTask<Result> ValidateAsync(CreateUserRequest request)
{
    return Validator.For(request)
        .RuleFor(x => x.Email)
            .NotEmpty("Email is required")
            .EmailAddress("Invalid email format")
        .ToResult();
}

// HandleAsync: Business rule validation
public async ValueTask<Result<User>> HandleAsync(CreateUserRequest request, CancellationToken ct)
{
    // Check if email already exists (requires database access)
    if (await _repository.EmailExistsAsync(request.Email, ct))
        return Result<User>.Failure("Email already in use");
    
    var user = new User(request.Email, request.Name);
    await _repository.AddAsync(user, ct);
    return Result<User>.Success(user);
}
```

### 4. Chain Related Validations

```csharp
.RuleFor(x => x.Email)
    .NotEmpty("Email is required")  // Check existence first
    .EmailAddress("Invalid email format")  // Then format
    .Must(email => email.EndsWith("@company.com"), "Must use company email")  // Then business rule
```

## Advanced Scenarios

### Conditional Validation

```csharp
public ValueTask<Result> ValidateAsync(UpdateUserRequest request)
{
    var validator = Validator.For(request)
        .RuleFor(x => x.Name)
            .NotEmpty("Name is required");
    
    // Only validate email if it's being changed
    if (!string.IsNullOrEmpty(request.NewEmail))
    {
        validator = validator
            .RuleFor(x => x.NewEmail)
                .EmailAddress("Invalid email format")
                .NotEqual(request.CurrentEmail, "New email must be different");
    }
    
    return validator.ToResult();
}
```

### Cross-Property Validation

```csharp
.RuleFor(x => x.ConfirmPassword)
    .Must(confirmPassword => confirmPassword == request.Password, 
          "Passwords must match")
```

## Performance Considerations

- Validation rules are executed synchronously
- Rules are evaluated in the order they are defined
- Validation stops at the first failure per property (short-circuits)
- All properties are validated even if one fails (to collect all errors)
- Minimal allocations - uses value types where possible

## See Also

- [Pipeline Hooks](Pipeline-Hooks.md) - Understanding the validation pipeline
- [Error Handling](Error-Handling.md) - Working with Result failures
- [Best Practices](Best-Practices.md) - General ResultR best practices
