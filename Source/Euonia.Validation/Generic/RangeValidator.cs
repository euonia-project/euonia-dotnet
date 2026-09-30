namespace Nerosoft.Euonia.Validation;

/// <summary>
/// Class RangeValidator.
/// Implements the <see cref="AbstractValidator{TValue}" />
/// </summary>
/// <typeparam name="TValue">The type of the t value.</typeparam>
/// <seealso cref="AbstractValidator{TValue}" />
public class RangeValidator<TValue> : AbstractValidator<TValue>
    where TValue : IComparable, IComparable<TValue>
{
    /// <summary>
    /// Gets the minimum.
    /// </summary>
    /// <value>The minimum.</value>
    public TValue Minimum { get; }

    /// <summary>
    /// Gets the maximum.
    /// </summary>
    /// <value>The maximum.</value>
    public TValue Maximum { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RangeValidator{TValue}"/> class.
    /// </summary>
    /// <param name="minimum">The minimum.</param>
    /// <param name="maximum">The maximum.</param>
    /// <param name="message">The error message.</param>
    /// <exception cref="ArgumentException">Minimum value could not greater than or equals maximum value.</exception>
    public RangeValidator(TValue minimum, TValue maximum, string message)
        : base(message)
    {
        if (minimum.CompareTo(maximum) >= 0)
        {
            throw new ArgumentException(Resources.IDS_MIN_CANNOT_GREATER_THAN_MAX);
        }

        Minimum = minimum;
        Maximum = maximum;
    }

    /// <summary>
    /// Validates the specified value.
    /// </summary>
    /// <param name="value">The value.</param>
    /// <returns><c>true</c> if the value is valid, <c>false</c> otherwise.</returns>
    public override bool Validate(TValue value)
    {
        return value.CompareTo(Minimum) >= 0 && value.CompareTo(Maximum) <= 0;
    }
}
