namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 提供当前 UTC <see cref="DateTime" />，以及用于缓存管理的时间相关方法。
/// 凡需要当前日期和时间时，都应使用此服务，而不是直接使用 <seealso cref="DateTime" />。
/// 它还使实现更易于测试，因为时间可以被模拟。
/// 实现 <see cref="IVolatileProvider" />
/// </summary>
/// <seealso cref="IVolatileProvider" />
public interface ICacheClock : IVolatileProvider
{
    /// <summary>
    /// 获取系统的当前 <see cref="DateTime" />，以 UTC 表示
    /// </summary>
    /// <value>当前 UTC 时间。</value>
    DateTime UtcNow { get; }

    /// <summary>
    /// 提供一个 <see cref="IVolatileToken" /> 实例，可用于在指定时长内缓存某些信息。
    /// </summary>
    /// <param name="duration">令牌必须保持有效的时长。</param>
    /// <returns>IVolatileToken。</returns>
    /// <example>
    /// 此示例演示如何通过返回名为 LoadVotes() 的方法结果来使用 <see cref="When" /> 方法，
    /// 该方法每 10 分钟才计算一次。
    /// <code>
    /// _cacheManager.Get("votes",
    /// ctx =&gt; {
    /// ctx.Monitor(_clock.When(TimeSpan.FromMinutes(10)));
    /// return LoadVotes();
    /// });
    /// </code></example>
    IVolatileToken When(TimeSpan duration);

    /// <summary>
    /// 提供一个 <see cref="IVolatileToken" /> 实例，可用于将某些内容缓存到指定的日期和时间之前。
    /// </summary>
    /// <param name="absoluteUtc">令牌必须保持有效的截止日期和时间。</param>
    /// <returns>IVolatileToken。</returns>
    /// <example>
    /// 此示例演示如何通过返回名为 LoadVotes() 的方法结果来使用 <see cref="WhenUtc" /> 方法，
    /// 该方法只计算一次，直到年末之前不再计算。
    /// <code>
    /// var endOfYear = _clock.UtcNow;
    /// endOfYear.Month = 12;
    /// endOfYear.Day = 31;
    /// _cacheManager.Get("votes",
    /// ctx =&gt; {
    /// ctx.Monitor(_clock.WhenUtc(endOfYear));
    /// return LoadVotes();
    /// });
    /// </code></example>
    IVolatileToken WhenUtc(DateTime absoluteUtc);
}