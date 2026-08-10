namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// 在 CacheManager 中，缓存背板用于保持进程内缓存与分布式缓存的同步。<br/>
/// 如果缓存管理器运行在访问同一分布式缓存的多个节点或应用程序中，并且进程内缓存被配置在分布式缓存句柄之前，
/// 则所有 Get 调用都会命中进程内缓存。<br/>
/// 现在，当某个客户端移除一个项时，其他所有客户端在进程内缓存中仍保留该缓存项。<br/>
/// 这可能导致错误和意外行为，因此缓存背板会向所有其他缓存客户端发送消息，要求它们也移除该项。
/// <para>
/// 相同的机制将应用于缓存的任何 Update、Put、Remove、Clear 或 ClearRegion 调用。
/// </para>
/// </summary>
public abstract class CacheBackplane : IDisposable
{
    /// <summary>
    /// 已发送的消息数。
    /// </summary>
    public static long MessagesSent = 0;

    /// <summary>
    /// 已接收的消息数。
    /// </summary>
    public static long MessagesReceived = 0;

    /// <summary>
    /// 已发送的消息块数。
    /// 消息以块的形式发送，以提高性能并减少网络流量。
    /// </summary>
    public static long SentChunks = 0;

    /// <summary>
    /// 初始化 <see cref="CacheBackplane" /> 类的新实例。
    /// </summary>
    /// <param name="configuration">缓存管理器配置。</param>
    /// <exception cref="ArgumentNullException">当 <c>configuration</c> 为 <c>null</c> 时抛出。</exception>
    protected CacheBackplane(CacheManagerConfiguration configuration)
    {
        Check.EnsureNotNull(configuration, nameof(configuration));
        CacheConfiguration = configuration;
        ConfigurationKey = configuration.BackplaneConfigurationKey;
    }

    /// <summary>
    /// 终结 <see cref="CacheBackplane"/> 类的实例。
    /// </summary>
    ~CacheBackplane()
    {
        Dispose(false);
    }

    /// <summary>
    /// 每当某个键的更改消息到达时触发此事件，
    /// 即表示另一个客户端更改了某个键。
    /// </summary>
    internal event EventHandler<CacheItemChangedEventArgs> Changed;

    /// <summary>
    /// 每当缓存清空消息到达时触发此事件。
    /// </summary>
    internal event EventHandler<EventArgs> Cleared;

    /// <summary>
    /// 每当清空区域消息到达时触发此事件。
    /// </summary>
    internal event EventHandler<RegionEventArgs> ClearedRegion;

    /// <summary>
    /// 每当某个键的移除消息到达时触发此事件。
    /// </summary>
    internal event EventHandler<CacheItemEventArgs> Removed;

    /// <summary>
    /// 获取缓存配置。
    /// </summary>
    /// <value>
    /// 缓存配置。
    /// </value>
    public CacheManagerConfiguration CacheConfiguration { get; }

    /// <summary>
    /// 获取要使用的配置名称。
    /// <para>此键可能用于查找缓存供应商特定的配置。</para>
    /// </summary>
    /// <value>配置键。</value>
    public string ConfigurationKey { get; }

    /// <summary>
    /// 执行与释放、重置非托管资源相关的应用程序定义任务。
    /// </summary>
    public void Dispose()
    {
        Dispose(true);

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// 向其他缓存客户端通知某个缓存键已更改。
    /// </summary>
    /// <param name="key">键.</param>
    /// <param name="action">操作。</param>
    public abstract void NotifyChange(string key, CacheItemChangedEventAction action);

    /// <summary>
    /// 向其他缓存客户端通知某个缓存键已更改。
    /// </summary>
    /// <param name="key">键.</param>
    /// <param name="region">区域。</param>
    /// <param name="action">操作。</param>
    public abstract void NotifyChange(string key, string region, CacheItemChangedEventAction action);

    /// <summary>
    /// 向其他缓存客户端通知缓存已清空。
    /// </summary>
    public abstract void NotifyClear();

    /// <summary>
    /// 向其他缓存客户端通知缓存区域已清空。
    /// </summary>
    /// <param name="region">区域。</param>
    public abstract void NotifyClearRegion(string region);

    /// <summary>
    /// 向其他缓存客户端通知某个缓存键已移除。
    /// </summary>
    /// <param name="key">键.</param>
    public abstract void NotifyRemove(string key);

    /// <summary>
    /// 向其他缓存客户端通知某个缓存键已移除。
    /// </summary>
    /// <param name="key">键.</param>
    /// <param name="region">区域。</param>
    public abstract void NotifyRemove(string key, string region);

    /// <summary>
    /// 为给定的 <paramref name="key"/> 发送更改消息。
    /// </summary>
    /// <param name="key">键.</param>
    /// <param name="action">操作。</param>
    protected internal void TriggerChanged(string key, CacheItemChangedEventAction action)
    {
        Changed?.Invoke(this, new CacheItemChangedEventArgs(key, action));
    }

    /// <summary>
    /// 为指定 <paramref name="region"/> 中给定的 <paramref name="key"/> 发送更改消息。
    /// </summary>
    /// <param name="key">键.</param>
    /// <param name="region">区域。</param>
    /// <param name="action">操作。</param>
    protected internal void TriggerChanged(string key, string region, CacheItemChangedEventAction action)
    {
        Changed?.Invoke(this, new CacheItemChangedEventArgs(key, region, action));
    }

    /// <summary>
    /// 发送缓存已清空的消息。
    /// </summary>
    protected internal void TriggerCleared()
    {
        Cleared?.Invoke(this, new EventArgs());
    }

    /// <summary>
    /// 为给定的 <paramref name="region"/> 发送区域已清空的消息。
    /// </summary>
    /// <param name="region">区域。</param>
    protected internal void TriggerClearedRegion(string region)
    {
        ClearedRegion?.Invoke(this, new RegionEventArgs(region));
    }

    /// <summary>
    /// 为给定的 <paramref name="key"/> 发送移除消息。
    /// </summary>
    /// <param name="key">键</param>
    protected internal void TriggerRemoved(string key)
    {
        Removed?.Invoke(this, new CacheItemEventArgs(key));
    }

    /// <summary>
    /// 为指定 <paramref name="region"/> 中给定的 <paramref name="key"/> 发送移除消息。
    /// </summary>
    /// <param name="key">键.</param>
    /// <param name="region">区域。</param>
    protected internal void TriggerRemoved(string key, string region)
    {
        Removed?.Invoke(this, new CacheItemEventArgs(key, region));
    }

    /// <summary>
    /// 释放非托管资源，并可选择性地释放托管资源。
    /// </summary>
    /// <param name="managed">
    /// <c>true</c> 表示同时释放托管和非托管资源；<c>false</c> 表示仅释放非托管资源。
    /// </param>
    protected virtual void Dispose(bool managed)
    {
    }
}

/// <summary>
/// 区域清空事件的事件参数。
/// </summary>
internal class RegionEventArgs : EventArgs
{
    /// <summary>
    /// 初始化 <see cref="RegionEventArgs" /> 类的新实例。
    /// </summary>
    /// <param name="region">区域。</param>
    public RegionEventArgs(string region)
    {
        Check.EnsureNotNull(region, nameof(region));
        Region = region;
    }

    /// <summary>
    /// 获取被清空的区域。
    /// </summary>
    /// <value>区域。</value>
    public string Region { get; }
}

/// <summary>
/// 基础缓存事件参数。
/// </summary>
internal class CacheItemEventArgs : EventArgs
{
    /// <summary>
    /// 初始化 <see cref="CacheItemEventArgs" /> 类的新实例。
    /// </summary>
    /// <param name="key">键.</param>
    public CacheItemEventArgs(string key)
    {
        Check.EnsureNotNull(key, nameof(key));
        Key = key;
    }

    /// <summary>
    /// 初始化 <see cref="CacheItemEventArgs" /> 类的新实例。
    /// </summary>
    /// <param name="key">键.</param>
    /// <param name="region">区域。</param>
    public CacheItemEventArgs(string key, string region)
        : this(key)
    {
        Check.EnsureNotNull(region, nameof(region));
        Region = region;
    }

    /// <summary>
    /// 获取键。
    /// </summary>
    public string Key { get; }

    /// <summary>
    /// 获取区域。
    /// </summary>
    public string Region { get; }
}

/// <summary>
/// 缓存更改事件的事件参数。
/// </summary>
internal class CacheItemChangedEventArgs : CacheItemEventArgs
{
    /// <summary>
    /// 初始化 <see cref="CacheItemChangedEventArgs" /> 类的新实例。
    /// </summary>
    /// <param name="key">键.</param>
    /// <param name="action">缓存操作。</param>
    public CacheItemChangedEventArgs(string key, CacheItemChangedEventAction action)
        : base(key)
    {
        Action = action;
    }

    /// <summary>
    /// 初始化 <see cref="CacheItemChangedEventArgs" /> 类的新实例。
    /// </summary>
    /// <param name="key">键.</param>
    /// <param name="region">区域。</param>
    /// <param name="action">缓存操作。</param>
    public CacheItemChangedEventArgs(string key, string region, CacheItemChangedEventAction action)
        : base(key, region)
    {
        Action = action;
    }

    /// <summary>
    /// 获取用于更改缓存中键的操作。
    /// </summary>
    public CacheItemChangedEventAction Action { get; }
}