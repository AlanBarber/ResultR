namespace ResultR.Validation;

/// <summary>
/// Metadata keys used for storing validation-related data in Result objects.
/// </summary>
public static class ValidationMetadataKeys
{
    /// <summary>
    /// Metadata key for storing a list of validation errors in a failed Result.
    /// </summary>
    public const string ValidationErrors = "ValidationErrors";
}
