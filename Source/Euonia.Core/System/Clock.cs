using System.Runtime.CompilerServices;

namespace System;

/// <summary>
/// 时间相关辅助类。
/// </summary>
public static class Clock
{
    /// <summary>
    /// 每毫秒的刻度数。
    /// </summary>
    public const long TicksPerMillisecond = 10000;

    /// <summary>
    /// 自 1970 年以来的刻度数。
    /// </summary>
    public const long UnixEpochTicks = TimeSpan.TicksPerDay * DAYS_TO1970;

    /// <summary>
    /// 自 1970 年以来的秒数。
    /// </summary>
    public const long UnixEpochSeconds = UnixEpochTicks / TimeSpan.TicksPerSecond;

    // 非闰年的天数
    private const int DAYS_PER_YEAR = 365;

    // 4 年的天数
    private const int DAYS_PER4_YEARS = DAYS_PER_YEAR * 4 + 1;       // 1461

    // 100 年的天数
    private const int DAYS_PER100_YEARS = DAYS_PER4_YEARS * 25 - 1;  // 36524

    // 400 年的天数
    private const int DAYS_PER400_YEARS = DAYS_PER100_YEARS * 4 + 1; // 146097

    // 从 0001年1月1日 到 1969年12月31日 的天数
    private const int DAYS_TO1970 = DAYS_PER400_YEARS * 4 + DAYS_PER100_YEARS * 3 + DAYS_PER4_YEARS * 17 + DAYS_PER_YEAR; // 719,162

    /// <summary>
    /// 计算表示自 1970 年以来毫秒数的时间戳。
    /// </summary>
    /// <returns>毫秒数。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetUnixTimestampMillis()
    {
        return (DateTime.UtcNow.Ticks - UnixEpochTicks) / TicksPerMillisecond;
    }

    /// <summary>
    /// 计算表示自 1970 年以来刻度数的时间戳。
    /// </summary>
    /// <returns>刻度数。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long GetUnixTimestampTicks()
    {
        return DateTime.UtcNow.Ticks - UnixEpochTicks;
    }

    /// <summary>
    /// 计算从 1970 年到给定 <paramref name="date"/> 的毫秒数。
    /// </summary>
    /// <param name="date"><see cref="DateTime"/> 基准时间。</param>
    /// <returns>自 1970 年以来的毫秒数。</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static long ToUnixTimestampMillis(DateTime date)
    {
        return (date.Ticks - UnixEpochTicks) / TicksPerMillisecond;
    }
}
