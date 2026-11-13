namespace Google.Protobuf;

/// <summary>
/// 表示十进制数的 Protobuf 消息类型，使用整数部分与纳秒部分组合表示数值。
/// </summary>
public sealed partial class DecimalValue
{
    private const decimal NANO_FACTOR = 1_000_000_000;

    /// <summary>
    /// 初始化 <see cref="DecimalValue"/> 类的新实例。
    /// </summary>
    /// <param name="units">整数部分，决定数值的符号与整数位。</param>
    /// <param name="nanos">纳秒部分（10<sup>-9</sup>），其符号应与 <paramref name="units"/> 一致。</param>
    public DecimalValue(long units, int nanos)
    {
        Units = units;
        Nanos = nanos;
    }

    /// <summary>
    /// 将 <see cref="DecimalValue"/> 隐式转换为 <see cref="decimal"/>。
    /// </summary>
    /// <param name="decimalValue">要转换的值。</param>
    /// <returns>由 <see cref="Units"/> 与 <see cref="Nanos"/> 按 10<sup>-9</sup> 比例组合而成的十进制数。</returns>
    public static implicit operator decimal(DecimalValue decimalValue)
    {
        return decimalValue.Units + decimalValue.Nanos / NANO_FACTOR;
    }

    /// <summary>
    /// 将 <see cref="DecimalValue"/> 隐式转换为可空的 <see cref="decimal"/>。
    /// </summary>
    /// <param name="decimalValue">要转换的值，可为 <see langword="null"/>。</param>
    /// <returns>若 <paramref name="decimalValue"/> 为 <see langword="null"/> 则为 <see langword="null"/>；否则为转换后的十进制数。</returns>
    public static implicit operator decimal?(DecimalValue decimalValue)
    {
        if (decimalValue == null)
            return null;
        return (decimal)decimalValue;
    }

    /// <summary>
    /// 将 <see cref="decimal"/> 隐式转换为 <see cref="DecimalValue"/>。
    /// </summary>
    /// <param name="value">要转换的十进制数。</param>
    /// <returns>由 <paramref name="value"/> 的整数部分与纳秒部分构造的实例。</returns>
    /// <remarks>小数部分按 10<sup>-9</sup> 精度截断，超出该精度的部分会丢失。</remarks>
    public static implicit operator DecimalValue(decimal value)
    {
        var units = decimal.ToInt64(value);
        var nanos = decimal.ToInt32((value - units) * NANO_FACTOR);
        return new DecimalValue(units, nanos);
    }

    /// <summary>
    /// 将可空的 <see cref="decimal"/> 隐式转换为可空的 <see cref="DecimalValue"/>。
    /// </summary>
    /// <param name="value">要转换的十进制数，可为 <see langword="null"/>。</param>
    /// <returns>若 <paramref name="value"/> 为 <see langword="null"/> 则为 <see langword="null"/>；否则为对应的 <see cref="DecimalValue"/>。</returns>
    public static implicit operator DecimalValue?(decimal? value)
    {
        if (value == null)
        {
            return null;
        }

        return value.Value;
    }
}