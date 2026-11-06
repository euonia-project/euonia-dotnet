namespace Nerosoft.Euonia.Validation;

/// <summary>
/// Represents the method/parameter/class should be validated.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Parameter | AttributeTargets.Method)]
public class ValidationAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationAttribute"/> class.
    /// </summary>
    public ValidationAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationAttribute"/> class.
    /// </summary>
    /// <param name="validatorType">Type of the validator.</param>
    public ValidationAttribute(Type validatorType)
        : this()
    {
        ValidatorType = validatorType;
    }

    /// <summary>
    /// Gets the type of the validator.
    /// </summary>
    /// <value>The type of the validator.</value>
    public Type? ValidatorType { get; }
}