using System.Linq.Expressions;
using ResultR;

namespace ResultR.Validation;

/// <summary>
/// Fluent builder that aggregates validation rules for a given instance.
/// </summary>
public sealed class ValidationBuilder<T>
{
    private readonly List<ValidationError> _errors = [];

    internal ValidationBuilder(T instance) => Instance = instance;

    /// <summary>
    /// The instance being validated.
    /// </summary>
    public T Instance { get; }

    /// <summary>
    /// Begins configuring validation rules for a specific property.
    /// </summary>
    /// <typeparam name="TProperty">The property type.</typeparam>
    /// <param name="expression">Expression selecting the property.</param>
    public RuleBuilder<T, TProperty> RuleFor<TProperty>(Expression<Func<T, TProperty>> expression)
        => new(this, expression);

    /// <summary>
    /// Finalizes validation and converts accumulated errors into a <see cref="Result"/>.
    /// </summary>
    public ValueTask<ResultR.Result> ToResult()
    {
        if (_errors.Count == 0)
        {
            return new(ResultR.Result.Success());
        }

        var failure = ResultR.Result
            .Failure("Validation failed")
            .WithMetadata(ValidationMetadataKeys.ValidationErrors, _errors.ToList());

        return new(failure);
    }

    internal ValidationBuilder<T> AddError(string propertyName, string errorMessage)
    {
        _errors.Add(new ValidationError(propertyName, errorMessage));
        return this;
    }
}
