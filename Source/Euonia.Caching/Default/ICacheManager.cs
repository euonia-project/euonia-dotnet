using System.Diagnostics.CodeAnalysis;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// Interface ICacheManager
/// </summary>
public interface ICacheManager
{
    /// <summary>
    /// Gets cached value with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns>缓存的项；未命中时返回 <typeparamref name="TResult"/> 的默认值（引用类型为 <c>null</c>）。</returns>
    TResult Get<TKey, TResult>(TKey key) where TKey : notnull;

    /// <summary>
    /// Gets the specified key.
    /// </summary>
    /// <typeparam name="TKey">The type of the t key.</typeparam>
    /// <typeparam name="TResult">The type of the t result.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="acquire">The acquire.</param>
    /// <returns>TResult.</returns>
    TResult GetOrAdd<TKey, TResult>(TKey key, Func<AcquireContext<TKey>, TResult> acquire) where TKey : notnull;

    /// <summary>
    /// Adds or updates cached value with the specified key.
    /// </summary>
    /// <param name="key"></param>
    /// <param name="acquire"></param>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TResult"></typeparam>
    /// <returns></returns>
    TResult AddOrUpdate<TKey, TResult>(TKey key, Func<AcquireContext<TKey>, TResult> acquire) where TKey : notnull;

    /// <summary>
    /// Try gets the specified key.
    /// </summary>
    /// <typeparam name="TKey">The type of the t key.</typeparam>
    /// <typeparam name="TResult">The type of the t result.</typeparam>
    /// <param name="key">The key.</param>
    /// <param name="result">The cached value of key.</param>
    /// <returns></returns>
    bool TryGet<TKey, TResult>(TKey key, [MaybeNullWhen(false)] out TResult result) where TKey : notnull;
}