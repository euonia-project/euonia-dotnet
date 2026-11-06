using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 提供 <see cref="ICacheManager"/> 的扩展方法，用于按字符串键读取、获取或添加以及添加或更新缓存值。
/// </summary>
public static class CacheManagerExtensions
{
    /// <summary>
    /// 锁集合。
    /// </summary>
    private static readonly ConcurrentDictionary<object, object> _locks = new();

    /// <summary>
    /// 获取指定键的缓存值。
    /// </summary>
    /// <typeparam name="TResult">缓存值的类型。</typeparam>
    /// <param name="manager">缓存管理器实例。</param>
    /// <param name="key">缓存键。</param>
    /// <returns>键对应的缓存值；键不存在时返回 <typeparamref name="TResult"/> 的默认值，引用类型为 <c>null</c>。</returns>
    public static TResult Get<TResult>(this ICacheManager manager, string key)
    {
        return manager.Get<string, TResult>(key);
    }

    /// <summary>
    /// 获取指定键的缓存值；若不存在，则使用 <paramref name="acquire"/> 创建并添加到缓存。
    /// </summary>
    /// <typeparam name="TResult">缓存值的类型。</typeparam>
    /// <param name="manager">缓存管理器实例。</param>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">缓存未命中时用于创建值的委托，接收当前键的获取上下文。</param>
    /// <returns>已存在的缓存值，或由 <paramref name="acquire"/> 新建并添加的值。</returns>
    public static TResult GetOrAdd<TResult>(this ICacheManager manager, string key, Func<AcquireContext<string>, TResult> acquire)
    {
        return manager.GetOrAdd(key, acquire);
    }

    /// <summary>
    /// 添加或更新指定键的缓存值。
    /// </summary>
    /// <typeparam name="TResult">缓存值的类型。</typeparam>
    /// <param name="manager">缓存管理器实例。</param>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">用于生成缓存值的委托，接收当前键的获取上下文。</param>
    /// <returns>添加或更新后的缓存值。</returns>
    public static TResult AddOrUpdate<TResult>(this ICacheManager manager, string key, Func<AcquireContext<string>, TResult> acquire)
    {
        return manager.AddOrUpdate(key, acquire);
    }

    /// <summary>
    /// 获取指定键的缓存值；若不存在，则使用 <paramref name="acquire"/> 创建并添加，可选地在进程内阻止同一键的并发调用。
    /// </summary>
    /// <typeparam name="TKey">缓存键的类型。</typeparam>
    /// <typeparam name="TResult">缓存值的类型。</typeparam>
    /// <param name="manager">缓存管理器实例。</param>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">缓存未命中时用于创建值的委托，接收当前键的获取上下文。</param>
    /// <param name="preventConcurrentCalls">为 <c>true</c> 时按 <paramref name="key"/> 加进程内锁，使相同键的调用串行执行；为 <c>false</c> 时直接访问缓存。</param>
    /// <returns>已存在的缓存值，或由 <paramref name="acquire"/> 新建并添加的值。</returns>
    /// <remarks>
    /// 启用并发保护时，锁对象按 <paramref name="key"/> 保存在静态字典中，并在进程生命周期内保留。
    /// </remarks>
    public static TResult GetOrAdd<TKey, TResult>(this ICacheManager manager, TKey key, Func<AcquireContext<TKey>, TResult> acquire, bool preventConcurrentCalls) where TKey : notnull
    {
        if (!preventConcurrentCalls)
        {
            return manager.GetOrAdd(key, acquire);
        }

        var lockKey = _locks.GetOrAdd(key, _ => new object());
        lock (lockKey)
        {
            return manager.GetOrAdd(key, acquire);
        }
    }

    /// <summary>
    /// 添加或更新指定键的缓存值，可选地在进程内阻止同一键的并发调用。
    /// </summary>
    /// <typeparam name="TKey">缓存键的类型。</typeparam>
    /// <typeparam name="TResult">缓存值的类型。</typeparam>
    /// <param name="manager">缓存管理器实例。</param>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">用于生成缓存值的委托，接收当前键的获取上下文。</param>
    /// <param name="preventConcurrentCalls">为 <c>true</c> 时按 <paramref name="key"/> 加进程内锁，使相同键的调用串行执行；为 <c>false</c> 时直接访问缓存。</param>
    /// <returns>添加或更新后的缓存值。</returns>
    /// <remarks>
    /// 启用并发保护时，锁对象按 <paramref name="key"/> 保存在静态字典中，并在进程生命周期内保留。
    /// </remarks>
    public static TResult AddOrUpdate<TKey, TResult>(this ICacheManager manager, TKey key, Func<AcquireContext<TKey>, TResult> acquire, bool preventConcurrentCalls) where TKey : notnull
    {
        if (!preventConcurrentCalls)
        {
            return manager.AddOrUpdate(key, acquire);
        }

        var lockKey = _locks.GetOrAdd(key, _ => new object());
        lock (lockKey)
        {
            return manager.AddOrUpdate(key, acquire);
        }
    }

    /// <summary>
    /// 获取指定键的缓存值；若不存在，则使用 <paramref name="acquire"/> 创建并添加，可选地在进程内阻止并发调用。
    /// </summary>
    /// <typeparam name="TResult">缓存值的类型。</typeparam>
    /// <param name="manager">缓存管理器实例。</param>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">缓存未命中时用于创建值的委托，接收当前键的获取上下文。</param>
    /// <param name="preventConcurrentCalls">为 <c>true</c> 时按 <paramref name="key"/> 加进程内锁，使相同键的调用串行执行；为 <c>false</c> 时直接访问缓存。</param>
    /// <returns>已存在的缓存值，或由 <paramref name="acquire"/> 新建并添加的值。</returns>
    public static TResult GetOrAdd<TResult>(this ICacheManager manager, string key, Func<AcquireContext<string>, TResult> acquire, bool preventConcurrentCalls)
    {
        return manager.GetOrAdd<string, TResult>(key, acquire, preventConcurrentCalls);
    }

    /// <summary>
    /// 添加或更新指定键的缓存值，可选地在进程内阻止同一键的并发调用。
    /// </summary>
    /// <typeparam name="TResult">缓存值的类型。</typeparam>
    /// <param name="manager">缓存管理器实例。</param>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">用于生成缓存值的委托，接收当前键的获取上下文。</param>
    /// <param name="preventConcurrentCalls">为 <c>true</c> 时按 <paramref name="key"/> 加进程内锁，使相同键的调用串行执行；为 <c>false</c> 时直接访问缓存。</param>
    /// <returns>添加或更新后的缓存值。</returns>
    public static TResult AddOrUpdate<TResult>(this ICacheManager manager, string key, Func<AcquireContext<string>, TResult> acquire, bool preventConcurrentCalls)
    {
        return manager.AddOrUpdate<string, TResult>(key, acquire, preventConcurrentCalls);
    }
}