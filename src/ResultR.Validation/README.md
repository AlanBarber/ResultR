# 🎯 ResultR.Validation

[![GitHub Release](https://img.shields.io/github/v/release/AlanBarber/ResultR)](https://github.com/AlanBarber/ResultR/releases)
[![GitHub Actions Workflow Status](https://img.shields.io/github/actions/workflow/status/AlanBarber/ResultR/release-resultr.yml)](https://github.com/AlanBarber/ResultR/actions/workflows/release-resultr.yml)
[![GitHub Downloads (all assets, all releases)](https://img.shields.io/github/downloads/AlanBarber/ResultR/total?label=github%20downloads)](https://github.com/AlanBarber/ResultR/releases)
[![NuGet Version](https://img.shields.io/nuget/v/ResultR.Validation)](https://www.nuget.org/packages/ResultR.Validation)
[![NuGet Downloads](https://img.shields.io/nuget/dt/ResultR.Validation?label=nuget%20downloads)](https://www.nuget.org/packages/ResultR.Validation)
[![GitHub License](https://img.shields.io/github/license/alanbarber/ResultR)](https://github.com/AlanBarber/ResultR/blob/main/LICENSE)

## 📖 Overview

Lightweight inline validation framework for ResultR. Define validation rules directly in your `ValidateAsync()` method using a fluent API, with seamless integration into ResultR's pipeline hooks.

## 📥 Installation

```bash
dotnet add package ResultR.Validation
```

## 🚀 Quick Start

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
        var user = new User(request.Email, request.Name, request.Age);
        // Save user...
        return Result<User>.Success(user);
    }
}
```

## Features

- **Zero ceremony** - No external validator classes, no DI registration for validators
- **Inline validation** - Define rules directly in `ValidateAsync()` using a fluent API
- **Seamless integration** - Works with ResultR's `IRequestHandler<TRequest>` and `IRequestHandler<TRequest, TResponse>`
- **Automatic result conversion** - Returns `Result.Success()` or `Result.Failure()` with aggregated errors
- **Comprehensive built-in rules** - String, numeric, collection, and custom validations

## Built-in Validation Rules

### String Validations
- `NotEmpty()` - Ensures string is not null, empty, or whitespace
- `NotNull()` - Ensures value is not null
- `MinLength(int)` - Minimum string length
- `MaxLength(int)` - Maximum string length
- `Length(int, int)` - String length range
- `Matches(string pattern)` - Regex pattern matching
- `EmailAddress()` - Valid email format

### Numeric Validations
- `GreaterThan(T)` - Value must be greater than comparison
- `GreaterThanOrEqualTo(T)` - Value must be greater than or equal to comparison
- `LessThan(T)` - Value must be less than comparison
- `LessThanOrEqualTo(T)` - Value must be less than or equal to comparison
- `Between(T, T)` - Value must be within range (inclusive)

### Collection Validations
- `NotEmpty()` - Collection must contain at least one element

### General Validations
- `NotNull()` - Value must not be null
- `Equal(T)` - Value must equal comparison
- `NotEqual(T)` - Value must not equal comparison
- `Must(Func<T, bool>)` - Custom predicate validation

## Custom Validation Rules

Use the `Must()` method for custom validation logic:

```csharp
public ValueTask<Result> ValidateAsync(CreateUserRequest request)
{
    return Validator.For(request)
        .RuleFor(x => x.Email)
            .NotEmpty("Email is required")
            .Must(email => email.EndsWith("@company.com"), "Must use company email")
        .RuleFor(x => x.Name)
            .Must(name => !name.Contains("admin", StringComparison.OrdinalIgnoreCase), 
                  "Name cannot contain 'admin'")
        .ToResult();
}
```

## Accessing Validation Errors

When validation fails, errors are stored in the `Result` metadata:

```csharp
var result = await mediator.SendAsync(request);

if (result.IsFailure)
{
    var errors = result.GetMetadataValueOrDefault<List<ValidationError>>(ValidationMetadataKeys.ValidationErrors);
    
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

## How It Works

1. `Validator.For(request)` creates a `ValidationBuilder<T>` for the request
2. Each `RuleFor()` call selects a property and returns a `RuleBuilder<T, TProperty>`
3. Validation methods (e.g., `NotEmpty()`, `MinLength()`) add rules to an internal list
4. `ToResult()` executes all rules and returns:
   - `Result.Success()` if all rules pass
   - `Result.Failure("Validation failed")` with errors in metadata if any rule fails

## ❓ Why ResultR.Validation?

Unlike FluentValidation which requires separate validator classes and DI registration, ResultR.Validation lets you define validation rules **inline** within your handler's `ValidateAsync()` method. This reduces ceremony and keeps validation logic close to your business logic.

## Links

- [GitHub Repository](https://github.com/AlanBarber/ResultR)
- [ResultR Core Package](https://www.nuget.org/packages/ResultR)
- [Documentation](https://github.com/AlanBarber/ResultR/wiki)

## 📋 Requirements

- .NET 10.0 or later
- C# 14.0 or later

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## 📄 License

ISC License - see the [LICENSE](https://github.com/AlanBarber/ResultR/blob/main/LICENSE) file for details.

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/AlanBarber/ResultR/issues)

---

Built with ❤️ for the C# / DotNet community.
