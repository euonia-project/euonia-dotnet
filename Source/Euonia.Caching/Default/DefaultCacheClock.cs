namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Clock 类。
/// 实现 <see cref="ICacheClock" />
/// </summary>
/// <seealso cref="ICacheClock" />
public class DefaultCacheClock : ICacheClock
{
    /// <summary>
    /// 获取系统的当前 <see cref="DateTime" />，以 UTC 表示
    /// </summary>
    /// <value>当前 UTC 时间。</value>
    public DateTime UtcNow => DateTime.UtcNow;

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
    /// ctx => {
    /// ctx.Monitor(_clock.When(TimeSpan.FromMinutes(10)));
    /// return LoadVotes();
    /// });
    /// </code></example>
    public IVolatileToken When(TimeSpan duration)
    {
        return new AbsoluteExpirationToken(this, duration);
    }

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
    public IVolatileToken WhenUtc(DateTime absoluteUtc)
    {
        return new AbsoluteExpirationToken(this, absoluteUtc);
    }

    /// <summary>
    /// 以固定 UTC 时刻为失效点的易变令牌，用于在指定时长或日期之前保持缓存有效。
    /// </summary>
    private class AbsoluteExpirationToken : IVolatileToken
    {
        /// <summary>
        /// 用于创建易变令牌的缓存时钟。
        /// </summary>
        private readonly ICacheClock _clock;

        /// <summary>
        /// 失效时间（UTC）
        /// </summary>
        private readonly DateTime _invalidateUtc;

        /// <summary>
        /// 初始化 <see cref="AbsoluteExpirationToken"/> 类的新实例。
        /// </summary>
        /// <param name="clock">时钟。</param>
        /// <param name="invalidateUtc">失效时间（UTC）。</param>
        public AbsoluteExpirationToken(ICacheClock clock, DateTime invalidateUtc)
        {
            _clock = clock;
            _invalidateUtc = invalidateUtc;
        }

        /// <summary>
        /// 初始化 <see cref="AbsoluteExpirationToken"/> 类的新实例。
        /// </summary>
        /// <param name="clock">时钟。</param>
        /// <param name="duration">时长。</param>
        public AbsoluteExpirationToken(ICacheClock clock, TimeSpan duration)
        {
            _clock = clock;
            _invalidateUtc = _clock.UtcNow.Add(duration);
        }

        /// <summary>
        /// 获取一个值，指示此实例当前是否有效。
        /// </summary>
        /// <value>如果此实例当前有效，则为 <c>true</c>；否则为 <c>false</c>。</value>
        public bool IsCurrent => _clock.UtcNow < _invalidateUtc;
    }
}