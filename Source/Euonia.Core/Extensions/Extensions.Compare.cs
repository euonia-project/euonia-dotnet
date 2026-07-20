public static partial class Extensions
{
    /// <summary>
    /// 检查值是否在最小值和最大值之间（含边界）。
    /// </summary>
    /// <param name="value">要检查的值。</param>
    /// <param name="minValue">最小值（含）。</param>
    /// <param name="maxValue">最大值（含）。</param>
    public static bool IsBetween<T>(this T value, T minValue, T maxValue)
        where T : IComparable<T>
    {
        return value.CompareTo(minValue) >= 0 && value.CompareTo(maxValue) <= 0;
    }

    /// <summary>
    /// 检查值是否不在指定范围内。
    /// </summary>
    /// <param name="value">要检查的值。</param>
    /// <param name="minValue">最小值。</param>
    /// <param name="maxValue">最大值。</param>
    public static bool IsNotInRange<T>(this T value, T minValue, T maxValue)
        where T : IComparable<T>
    {
        return value.CompareTo(minValue) < 0 && value.CompareTo(maxValue) > 0;
    }
}