namespace Nerosoft.Euonia.Caching;

/// <summary>
/// <see cref="AcquireContext{TKey}"/> 类。
/// 实现 <see cref="IAcquireContext" />。
/// </summary>
/// <typeparam name="TKey">键的类型。</typeparam>
/// <seealso cref="IAcquireContext" />
public class AcquireContext<TKey> : IAcquireContext
{
    /// <summary>
    /// 初始化 <see cref="AcquireContext{TKey}"/> 类的新实例。
    /// </summary>
    /// <param name="key">键。</param>
    /// <param name="monitor">监视器。</param>
    public AcquireContext(TKey key, Action<IVolatileToken> monitor)
    {
        Key = key;
        Monitor = monitor;
    }

    /// <summary>
    /// 获取键。
    /// </summary>
    /// <value>键。</value>
    public TKey Key { get; }

    /// <summary>
    /// 获取监视器。
    /// </summary>
    /// <value>监视器。</value>
    public Action<IVolatileToken> Monitor { get; }
}

/// <summary>
/// 基于 lambda 的 "IAcquireContext" 简单实现。
/// 实现 <see cref="IAcquireContext" />。
/// </summary>
/// <seealso cref="IAcquireContext" />
public class SimpleAcquireContext : IAcquireContext
{
    /// <summary>
    /// 初始化 <see cref="SimpleAcquireContext"/> 类的新实例。
    /// </summary>
    /// <param name="monitor">监视器。</param>
    public SimpleAcquireContext(Action<IVolatileToken> monitor)
    {
        Monitor = monitor;
    }

    /// <summary>
    /// 获取监视器。
    /// </summary>
    /// <value>监视器。</value>
    public Action<IVolatileToken> Monitor { get; }
}