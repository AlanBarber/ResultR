namespace ResultR.Validation;

/// <summary>
/// Entry point for creating inline validators against a specific request instance.
/// </summary>
public static class Validator
{
    /// <summary>
    /// Creates a <see cref="ValidationBuilder{T}"/> for the provided instance.
    /// </summary>
    /// <typeparam name="T">The type of the instance being validated.</typeparam>
    /// <param name="instance">The instance to validate.</param>
    /// <returns>A validation builder that can be used to define rules.</returns>
    public static ValidationBuilder<T> For<T>(T instance)
    {
        if (instance is null)
        {
            throw new ArgumentNullException(nameof(instance));
        }

        return new ValidationBuilder<T>(instance);
    }
}
