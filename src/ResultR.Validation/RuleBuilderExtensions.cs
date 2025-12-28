using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ResultR.Validation;

/// <summary>
/// Extension methods providing common validation rules for <see cref="RuleBuilder{T,TProperty}"/>.
/// </summary>
public static class RuleBuilderExtensions
{
    private const string DefaultRequiredMessage = "{0} is required.";
    private const string DefaultInvalidMessage = "{0} is invalid.";

    private static string FormatRequired(string? message, string propertyName)
        => message ?? string.Format(DefaultRequiredMessage, propertyName);

    private static bool IsNull<TProperty>(TProperty value)
    {
        object? boxed = value;
        return boxed is null;
    }

    /// <summary>
    /// Validates that the property value is not null.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, TProperty> NotNull<T, TProperty>(
        this RuleBuilder<T, TProperty> rule,
        string? message = null)
    {
        if (rule.GetValue() is null)
        {
            rule.AddError(FormatRequired(message, rule.PropertyName));
        }

        return rule;
    }

    /// <summary>
    /// Validates that the string property is not null, empty, or whitespace.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, string?> NotEmpty<T>(
        this RuleBuilder<T, string?> rule,
        string? message = null)
    {
        if (string.IsNullOrWhiteSpace(rule.GetValue()))
        {
            rule.AddError(FormatRequired(message, rule.PropertyName));
        }

        return rule;
    }

    /// <summary>
    /// Validates that the collection property is not null or empty.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TElement">The collection element type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, IEnumerable<TElement>> NotEmpty<T, TElement>(
        this RuleBuilder<T, IEnumerable<TElement>> rule,
        string? message = null)
    {
        var value = rule.GetValue();
        if (value is null || !value.Any())
        {
            rule.AddError(FormatRequired(message, rule.PropertyName));
        }

        return rule;
    }

    /// <summary>
    /// Validates that the string property has a minimum length.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="minLength">The minimum allowed length.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, string?> MinLength<T>(
        this RuleBuilder<T, string?> rule,
        int minLength,
        string? message = null)
    {
        var value = rule.GetValue();
        if (value is not null && value.Length < minLength)
        {
            rule.AddError(message ?? $"{rule.PropertyName} must be at least {minLength} characters.");
        }

        return rule;
    }

    /// <summary>
    /// Validates that the string property does not exceed a maximum length.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, string?> MaxLength<T>(
        this RuleBuilder<T, string?> rule,
        int maxLength,
        string? message = null)
    {
        var value = rule.GetValue();
        if (value is not null && value.Length > maxLength)
        {
            rule.AddError(message ?? $"{rule.PropertyName} must be {maxLength} characters or fewer.");
        }

        return rule;
    }

    /// <summary>
    /// Validates that the string property length is within a specified range.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="minLength">The minimum allowed length.</param>
    /// <param name="maxLength">The maximum allowed length.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, string?> Length<T>(
        this RuleBuilder<T, string?> rule,
        int minLength,
        int maxLength,
        string? message = null)
    {
        var value = rule.GetValue();
        if (value is null)
        {
            return rule;
        }

        if (value.Length < minLength || value.Length > maxLength)
        {
            rule.AddError(message ?? $"{rule.PropertyName} must be between {minLength} and {maxLength} characters.");
        }

        return rule;
    }

    /// <summary>
    /// Validates that the string property matches a regular expression pattern.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="pattern">The regular expression pattern to match.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <param name="options">Regular expression options.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, string?> Matches<T>(
        this RuleBuilder<T, string?> rule,
        string pattern,
        string? message = null,
        RegexOptions options = RegexOptions.None)
    {
        var value = rule.GetValue();
        if (value is null)
        {
            return rule;
        }

        if (!Regex.IsMatch(value, pattern, options))
        {
            rule.AddError(message ?? string.Format(DefaultInvalidMessage, rule.PropertyName));
        }

        return rule;
    }

    /// <summary>
    /// Validates that the string property is a valid email address format.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, string?> EmailAddress<T>(
        this RuleBuilder<T, string?> rule,
        string? message = null)
    {
        var value = rule.GetValue();
        if (value is null)
        {
            return rule;
        }

        const string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
        if (!Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase))
        {
            rule.AddError(message ?? $"{rule.PropertyName} must be a valid email address.");
        }

        return rule;
    }

    /// <summary>
    /// Validates that the property value is greater than the specified comparison value.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="comparisonValue">The value to compare against.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, TProperty> GreaterThan<T, TProperty>(
        this RuleBuilder<T, TProperty> rule,
        TProperty comparisonValue,
        string? message = null)
        where TProperty : IComparable<TProperty>
    {
        var value = rule.GetValue();
        if (IsNull(value))
        {
            return rule;
        }

        if (value.CompareTo(comparisonValue) <= 0)
        {
            rule.AddError(message ?? $"{rule.PropertyName} must be greater than {comparisonValue}.");
        }

        return rule;
    }

    /// <summary>
    /// Validates that the property value is greater than or equal to the specified comparison value.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="comparisonValue">The value to compare against.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, TProperty> GreaterThanOrEqualTo<T, TProperty>(
        this RuleBuilder<T, TProperty> rule,
        TProperty comparisonValue,
        string? message = null)
        where TProperty : IComparable<TProperty>
    {
        var value = rule.GetValue();
        if (IsNull(value))
        {
            return rule;
        }

        if (value.CompareTo(comparisonValue) < 0)
        {
            rule.AddError(message ?? $"{rule.PropertyName} must be greater than or equal to {comparisonValue}.");
        }

        return rule;
    }

    /// <summary>
    /// Validates that the property value is less than the specified comparison value.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="comparisonValue">The value to compare against.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, TProperty> LessThan<T, TProperty>(
        this RuleBuilder<T, TProperty> rule,
        TProperty comparisonValue,
        string? message = null)
        where TProperty : IComparable<TProperty>
    {
        var value = rule.GetValue();
        if (IsNull(value))
        {
            return rule;
        }

        if (value.CompareTo(comparisonValue) >= 0)
        {
            rule.AddError(message ?? $"{rule.PropertyName} must be less than {comparisonValue}.");
        }

        return rule;
    }

    /// <summary>
    /// Validates that the property value is less than or equal to the specified comparison value.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="comparisonValue">The value to compare against.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, TProperty> LessThanOrEqualTo<T, TProperty>(
        this RuleBuilder<T, TProperty> rule,
        TProperty comparisonValue,
        string? message = null)
        where TProperty : IComparable<TProperty>
    {
        var value = rule.GetValue();
        if (IsNull(value))
        {
            return rule;
        }

        if (value.CompareTo(comparisonValue) > 0)
        {
            rule.AddError(message ?? $"{rule.PropertyName} must be less than or equal to {comparisonValue}.");
        }

        return rule;
    }

    /// <summary>
    /// Validates that the property value is within the specified range (inclusive).
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="minValue">The minimum allowed value.</param>
    /// <param name="maxValue">The maximum allowed value.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, TProperty> Between<T, TProperty>(
        this RuleBuilder<T, TProperty> rule,
        TProperty minValue,
        TProperty maxValue,
        string? message = null)
        where TProperty : IComparable<TProperty>
    {
        var value = rule.GetValue();
        if (IsNull(value))
        {
            return rule;
        }

        if (value.CompareTo(minValue) < 0 || value.CompareTo(maxValue) > 0)
        {
            rule.AddError(message ?? $"{rule.PropertyName} must be between {minValue} and {maxValue}.");
        }

        return rule;
    }

    /// <summary>
    /// Validates that the property value equals the specified comparison value.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="comparisonValue">The value to compare against.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, TProperty> Equal<T, TProperty>(
        this RuleBuilder<T, TProperty> rule,
        TProperty comparisonValue,
        string? message = null)
        where TProperty : IEquatable<TProperty>
    {
        if (!EqualityComparer<TProperty>.Default.Equals(rule.GetValue(), comparisonValue))
        {
            rule.AddError(message ?? $"{rule.PropertyName} must equal {comparisonValue}.");
        }

        return rule;
    }

    /// <summary>
    /// Validates that the property value does not equal the specified disallowed value.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="disallowedValue">The value that is not allowed.</param>
    /// <param name="message">Optional custom error message.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, TProperty> NotEqual<T, TProperty>(
        this RuleBuilder<T, TProperty> rule,
        TProperty disallowedValue,
        string? message = null)
        where TProperty : IEquatable<TProperty>
    {
        if (EqualityComparer<TProperty>.Default.Equals(rule.GetValue(), disallowedValue))
        {
            rule.AddError(message ?? $"{rule.PropertyName} must not equal {disallowedValue}.");
        }

        return rule;
    }

    /// <summary>
    /// Validates that the property value satisfies a custom predicate.
    /// </summary>
    /// <typeparam name="T">The type being validated.</typeparam>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="rule">The rule builder.</param>
    /// <param name="predicate">The predicate function that must return true for the value to be valid.</param>
    /// <param name="message">The error message to use if validation fails.</param>
    /// <returns>The rule builder for method chaining.</returns>
    public static RuleBuilder<T, TProperty> Must<T, TProperty>(
        this RuleBuilder<T, TProperty> rule,
        Func<TProperty, bool> predicate,
        string message)
    {
        if (!predicate(rule.GetValue()))
        {
            rule.AddError(message);
        }

        return rule;
    }
}
