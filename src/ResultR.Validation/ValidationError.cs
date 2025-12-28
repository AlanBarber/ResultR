namespace ResultR.Validation;

/// <summary>
/// Represents a single validation error for a specific property.
/// </summary>
/// <param name="PropertyName">Name of the property that failed validation.</param>
/// <param name="ErrorMessage">The associated validation message.</param>
public sealed record ValidationError(string PropertyName, string ErrorMessage);
