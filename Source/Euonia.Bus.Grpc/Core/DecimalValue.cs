namespace Google.Protobuf;

public sealed partial class DecimalValue
{
    private const decimal NANO_FACTOR = 1_000_000_000;

    /// <summary>
    /// Initializes a new instance of the <see cref="DecimalValue"/> class.
    /// </summary>
    /// <param name="units"></param>
    /// <param name="nanos"></param>
    public DecimalValue(long units, int nanos)
    {
        Units = units;
        Nanos = nanos;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="decimalValue"></param>
    /// <returns></returns>
    public static implicit operator decimal(DecimalValue decimalValue)
    {
        return decimalValue.Units + decimalValue.Nanos / NANO_FACTOR;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="decimalValue"></param>
    /// <returns></returns>
    public static implicit operator decimal?(DecimalValue decimalValue)
    {
        if (decimalValue == null)
            return null;
        return (decimal)decimalValue;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator DecimalValue(decimal value)
    {
        var units = decimal.ToInt64(value);
        var nanos = decimal.ToInt32((value - units) * NANO_FACTOR);
        return new DecimalValue(units, nanos);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static implicit operator DecimalValue(decimal? value)
    {
        if (value == null)
        {
            return null;
        }

        return value.Value;
    }
}