using System.Linq.Expressions;
using ResultR;

namespace ResultR.Validation;

/// <summary>
/// Fluent builder for configuring validation rules on a single property.
/// </summary>
public sealed class RuleBuilder<T, TProperty>
{
    private readonly ValidationBuilder<T> _builder;
    private readonly Func<T, TProperty> _accessor;

    internal RuleBuilder(ValidationBuilder<T> builder, Expression<Func<T, TProperty>> expression)
    {
        _builder = builder;
        _accessor = expression.Compile();
        PropertyName = ExpressionUtilities.GetMemberName(expression);
    }

    internal string PropertyName { get; }

    internal RuleBuilder<T, TProperty> AddError(string message)
    {
        _builder.AddError(PropertyName, message);
        return this;
    }

    internal TProperty GetValue() => _accessor(_builder.Instance);

    /// <summary>
    /// Allows chaining validation for another property without breaking the fluent pipeline.
    /// </summary>
    public RuleBuilder<T, TNewProperty> RuleFor<TNewProperty>(Expression<Func<T, TNewProperty>> expression)
        => _builder.RuleFor(expression);

    /// <summary>
    /// Completes validation and converts accumulated errors into a <see cref="Result"/>.
    /// </summary>
    public ValueTask<ResultR.Result> ToResult() => _builder.ToResult();
}
