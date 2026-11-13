using System.Diagnostics.CodeAnalysis;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 定义按泛型键读写缓存项的缓存管理器契约。
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// 获取指定键的缓存值。
    /// </summary>
    /// <typeparam name="TKey">缓存键的类型，必须为非 null 类型。</typeparam>
    /// <typeparam name="TResult">缓存值的类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <returns>缓存的项；未命中时返回 <typeparamref name="TResult"/> 的默认值（引用类型为 <see langword="null"/>）。</returns>
    TResult Get<TKey, TResult>(TKey key) where TKey : notnull;

    /// <summary>
    /// 获取指定键的缓存值；不存在时通过委托获取并写入缓存。
    /// </summary>
    /// <typeparam name="TKey">缓存键的类型，必须为非 null 类型。</typeparam>
    /// <typeparam name="TResult">缓存值的类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">缓存未命中时用于生成缓存值的委托，接收包含键与令牌监视器的 <see cref="AcquireContext{TKey}"/>。</param>
    /// <returns>键对应的缓存值；未命中时返回 <paramref name="acquire"/> 的执行结果，并将其写入缓存。</returns>
    TResult GetOrAdd<TKey, TResult>(TKey key, Func<AcquireContext<TKey>, TResult> acquire) where TKey : notnull;

    /// <summary>
    /// 添加或更新指定键的缓存值。
    /// </summary>
    /// <typeparam name="TKey">缓存键的类型，必须为非 null 类型。</typeparam>
    /// <typeparam name="TResult">缓存值的类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="acquire">用于生成新缓存值的委托，接收包含键与令牌监视器的 <see cref="AcquireContext{TKey}"/>。</param>
    /// <returns>添加或更新后的缓存值，即 <paramref name="acquire"/> 的执行结果。</returns>
    TResult AddOrUpdate<TKey, TResult>(TKey key, Func<AcquireContext<TKey>, TResult> acquire) where TKey : notnull;

    /// <summary>
    /// 尝试获取指定键的缓存值。
    /// </summary>
    /// <typeparam name="TKey">缓存键的类型，必须为非 null 类型。</typeparam>
    /// <typeparam name="TResult">缓存值的类型。</typeparam>
    /// <param name="key">缓存键。</param>
    /// <param name="result">找到时为键对应的缓存值；未找到时为 <typeparamref name="TResult"/> 的默认值。</param>
    /// <returns>找到且该项的易变令牌均未失效时返回 <see langword="true"/>；否则返回 <see langword="false"/>。</returns>
    /// <remarks>仅当缓存项存在且其所有 <see cref="IVolatileToken"/> 均处于有效状态时才视为命中。</remarks>
    bool TryGet<TKey, TResult>(TKey key, [MaybeNullWhen(false)] out TResult result) where TKey : notnull;
}