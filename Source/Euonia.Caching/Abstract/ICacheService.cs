// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 缓存的基服务契约。
/// </summary>
public interface ICacheService
{
	/// <summary>
	/// 获取指定键的缓存值。
	/// </summary>
	/// <typeparam name="TValue">值的类型。</typeparam>
	/// <param name="key">缓存项键。</param>
	/// <returns>指定键的缓存值；键不存在时返回 <typeparamref name="TValue"/> 的默认值。</returns>
	TValue Get<TValue>(string key);

	/// <summary>
	/// 尝试获取指定键的缓存值。
	/// </summary>
	/// <param name="key">要检索的缓存项键。</param>
	/// <param name="value">找到时的缓存值；否则为默认值。</param>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <returns>如果键存在于缓存中则为 <c>true</c>；否则为 <c>false</c>。</returns>
	bool TryGet<TValue>(string key, out TValue value);

	/// <summary>
	/// 获取已存在的值，或在指定键不存在时添加新值。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <param name="key">缓存项键。</param>
	/// <param name="factory">当键不存在时用于创建值的工厂函数。</param>
	/// <param name="timeout">缓存条目的可选超时时间。</param>
	/// <returns>缓存值，或由工厂新创建的值。</returns>
	TValue GetOrAdd<TValue>(string key, Func<TValue> factory, TimeSpan? timeout = null);

	/// <summary>
	/// 获取已存在的值，或在指定键不存在时以绝对过期时间添加新值。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <param name="key">缓存项键。</param>
	/// <param name="factory">当键不存在时用于创建值的工厂函数。</param>
	/// <param name="timeout">缓存条目的绝对过期时间。</param>
	/// <param name="isUtcTime">指示超时时间是否为 UTC 时间；默认为 <c>true</c>。</param>
	/// <returns>缓存值，或由工厂新创建的值。</returns>
	TValue GetOrAdd<TValue>(string key, Func<TValue> factory, DateTime timeout, bool isUtcTime = true);

	/// <summary>
	/// 使用工厂函数添加或更新指定键的缓存值。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <param name="key">缓存项键。</param>
	/// <param name="factory">用于创建或更新值的工厂函数。</param>
	/// <param name="timeout">缓存条目的可选超时时间。</param>
	/// <returns>新创建或更新后的缓存值。</returns>
	TValue AddOrUpdate<TValue>(string key, Func<TValue> factory, TimeSpan? timeout = null);

	/// <summary>
	/// 使用工厂函数以绝对过期时间添加或更新指定键的缓存值。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <param name="key">缓存项键。</param>
	/// <param name="factory">用于创建或更新值的工厂函数。</param>
	/// <param name="timeout">缓存条目的绝对过期时间。</param>
	/// <param name="isUtcTime">指示超时时间是否为 UTC 时间；默认为 <c>true</c>。</param>
	/// <returns>新创建或更新后的缓存值。</returns>
	TValue AddOrUpdate<TValue>(string key, Func<TValue> factory, DateTime timeout, bool isUtcTime = true);

	/// <summary>
	/// 使用直接值添加或更新指定键的缓存值。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <param name="key">缓存项键。</param>
	/// <param name="value">要缓存的值。</param>
	/// <param name="timeout">缓存条目的可选超时时间。</param>
	/// <returns>缓存值。</returns>
	TValue AddOrUpdate<TValue>(string key, TValue value, TimeSpan? timeout = null);

	/// <summary>
	/// 使用直接值和绝对过期时间添加或更新指定键的缓存值。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <param name="key">缓存项键。</param>
	/// <param name="value">要缓存的值。</param>
	/// <param name="timeout">缓存条目的绝对过期时间。</param>
	/// <param name="isUtcTime">指示超时时间是否为 UTC 时间；默认为 <c>true</c>。</param>
	/// <returns>缓存值。</returns>
	TValue AddOrUpdate<TValue>(string key, TValue value, DateTime timeout, bool isUtcTime = true);

	/// <summary>
	/// 使用 <see cref="CacheItem{TValue}"/> 实例添加或更新缓存值。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <param name="item">包含键、值和过期设置的缓存项。</param>
	/// <returns>缓存项中的缓存值。</returns>
	TValue AddOrUpdate<TValue>(CacheItem<TValue> item);

	/// <summary>
	/// 移除指定键的缓存值。
	/// </summary>
	/// <remarks>
	/// 移除是**类型无关**的：条目只由键标识，<typeparamref name="TValue"/> 不参与匹配。
	/// 因此 <c>Remove&lt;object&gt;(key)</c> 可以移除以任意具体类型写入的条目
	/// ——缓存失效方通常并不知道写入时的泛型实参。
	/// <para>
	/// 注意与读取的差异：<see cref="Get{TValue}"/> / <see cref="TryGet{TValue}"/> 要求
	/// <typeparamref name="TValue"/> 与写入时一致（条目以 <c>CacheItem&lt;TValue&gt;</c> 存储，该类型不协变）。
	/// </para>
	/// </remarks>
	/// <typeparam name="TValue">写入时使用的值类型；仅用于选择缓存管理器，不参与匹配。</typeparam>
	/// <param name="key">要移除的缓存项键。</param>
	/// <returns>如果键存在且已移除则为 <c>true</c>；否则为 <c>false</c>。</returns>
	bool Remove<TValue>(string key);

	/// <summary>
	/// 异步尝试获取指定键的缓存值。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <param name="key">要检索的缓存项键。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <returns>包含成功标志及找到时的缓存值的元组。</returns>
	Task<Tuple<bool, TValue>> TryGetAsync<TValue>(string key, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步获取已存在的值，或在指定键不存在时添加新值。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <param name="key">缓存项键。</param>
	/// <param name="factory">当键不存在时用于创建值的异步工厂函数。</param>
	/// <param name="timeout">缓存条目的可选超时时间。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <returns>缓存值，或由工厂新创建的值。</returns>
	Task<TValue> GetOrAddAsync<TValue>(string key, Func<Task<TValue>> factory, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步获取已存在的值，或在指定键不存在时以绝对过期时间添加新值。
	/// </summary>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <param name="key">缓存项键。</param>
	/// <param name="factory">当键不存在时用于创建值的异步工厂函数。</param>
	/// <param name="timeout">缓存条目的绝对过期时间。</param>
	/// <param name="isUtcTime">指示超时时间是否为 UTC 时间；默认为 <c>true</c>。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <returns>缓存值，或由工厂新创建的值。</returns>
	Task<TValue> GetOrAddAsync<TValue>(string key, Func<Task<TValue>> factory, DateTime timeout, bool isUtcTime = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步添加或更新指定键的缓存值。
	/// </summary>
	/// <param name="key">缓存项键。</param>
	/// <param name="factory">用于创建或更新值的异步工厂函数。</param>
	/// <param name="timeout">缓存条目的可选超时时间。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <returns>新创建或更新后的缓存值。</returns>
	Task<TValue> AddOrUpdateAsync<TValue>(string key, Func<Task<TValue>> factory, TimeSpan? timeout = null, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步添加或更新指定键的缓存值。
	/// </summary>
	/// <param name="key">缓存项键。</param>
	/// <param name="factory">用于创建或更新值的异步工厂函数。</param>
	/// <param name="timeout">缓存条目的绝对过期时间。</param>
	/// <param name="isUtcTime">指示超时时间是否为 UTC 时间；默认为 <c>true</c>。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <returns>新创建或更新后的缓存值。</returns>
	Task<TValue> AddOrUpdateAsync<TValue>(string key, Func<Task<TValue>> factory, DateTime timeout, bool isUtcTime = true, CancellationToken cancellationToken = default);

	/// <summary>
	/// 异步添加或更新指定键的缓存值。
	/// </summary>
	/// <param name="factory">返回要缓存的 <see cref="CacheItem{TValue}"/> 的异步工厂函数。</param>
	/// <param name="cancellationToken">用于取消操作的取消令牌。</param>
	/// <typeparam name="TValue">缓存值的类型。</typeparam>
	/// <returns>新创建或更新后的缓存值。</returns>
	Task<TValue> AddOrUpdateAsync<TValue>(Func<Task<CacheItem<TValue>>> factory, CancellationToken cancellationToken = default);
}