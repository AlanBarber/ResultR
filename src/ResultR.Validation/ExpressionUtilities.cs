using System.Linq.Expressions;

namespace ResultR.Validation;

internal static class ExpressionUtilities
{
    public static string GetMemberName<T, TProperty>(Expression<Func<T, TProperty>> expression)
    {
        var memberExpression = expression.Body switch
        {
            MemberExpression member => member,
            UnaryExpression { Operand: MemberExpression inner } => inner,
            _ => null
        };

        if (memberExpression is null)
        {
            throw new ArgumentException("Expression must target a property or field", nameof(expression));
        }

        return memberExpression.Member.Name;
    }
}
