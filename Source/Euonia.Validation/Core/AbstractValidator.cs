namespace Nerosoft.Euonia.Validation;

/// <summary>
/// The abstract validator class implements the <see cref="IObjectValidator{TValue}" />
/// </summary>
/// <typeparam name="TValue">The type of the t value.</typeparam>
/// <seealso cref="IObjectValidator{TValue}" />
public abstract class AbstractValidator<TValue> : IObjectValidator<TValue>
{
    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    /// <value>The error message.</value>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractValidator{TValue}"/> class.
    /// </summary>
    protected AbstractValidator()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AbstractValidator{TValue}"/> class.
    /// </summary>
    /// <param name="message">The error message.</param>
    protected AbstractValidator(string message)
        : this()
    {
        Message = message;
    }

    /// <summary>
    /// Validates the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> if XXXX, <c>false</c> otherwise.</returns>
    public abstract bool Validate(TValue value);
}