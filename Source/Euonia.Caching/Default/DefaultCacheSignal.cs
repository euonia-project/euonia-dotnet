// ReSharper disable UnusedType.Global

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// <see cref="DefaultCacheSignal"/> 类。
/// 实现 <see cref="ICacheSignal" />。
/// </summary>
/// <seealso cref="ICacheSignal" />
public class DefaultCacheSignal : ICacheSignal
{
    /// <summary>
    /// 令牌集合。
    /// </summary>
    private readonly Dictionary<object, VolatileToken> _tokens = new();

    /// <summary>
    /// 触发指定的信号。
    /// </summary>
    /// <typeparam name="T">信号类型。</typeparam>
    /// <param name="signal">信号。</param>
    public void Trigger<T>(T signal)
    {
        lock (_tokens)
        {
            if (!_tokens.TryGetValue(signal!, out var token))
            {
                return;
            }

            _tokens.Remove(signal!);
            token.Trigger();
        }
    }

    /// <summary>
    /// 为指定的信号创建令牌。
    /// </summary>
    /// <typeparam name="T">信号类型。</typeparam>
    /// <param name="signal">信号。</param>
    /// <returns>IVolatileToken。</returns>
    public IVolatileToken When<T>(T signal)
    {
        lock (_tokens)
        {
            if (_tokens.TryGetValue(signal!, out var token))
            {
                return token;
            }

            token = new VolatileToken();
            _tokens[signal!] = token;

            return token;
        }
    }

    /// <summary>
    /// 由 <see cref="DefaultCacheSignal"/> 按信号登记、触发后失效的易变令牌。
    /// </summary>
    private class VolatileToken : IVolatileToken
    {
        /// <summary>
        /// 初始化 <see cref="VolatileToken"/> 类的新实例。
        /// </summary>
        public VolatileToken()
        {
            IsCurrent = true;
        }

        /// <summary>
        /// 获取一个值，指示此实例是否为当前状态。
        /// </summary>
        /// <value>若此实例为当前状态，则为 <c>true</c>；否则为 <c>false</c>。</value>
        public bool IsCurrent { get; private set; }

        /// <summary>
        /// 触发此实例。
        /// </summary>
        public void Trigger()
        {
            IsCurrent = false;
        }
    }
}