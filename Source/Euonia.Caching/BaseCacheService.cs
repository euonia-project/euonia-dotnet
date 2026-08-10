namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 为管理带键前缀支持的缓存操作提供基础实现的缓存服务基类。
/// </summary>
/// <remarks>
/// 此抽象类作为缓存服务实现的基础，提供键重写功能以确保缓存操作中键命名约定的一致性。
/// 派生类必须实现 <see cref="GetCacheManager{T}"/> 方法以提供具体的缓存管理器实例。
/// </remarks>
public abstract class BaseCacheService
{
	/// <summary>
	/// 获取或设置将添加到所有缓存键前面的前缀。
	/// </summary>
	/// <value>
	/// 表示键前缀的字符串。如果为 <see langword="null"/> 或空，则不向前缀添加任何内容。
	/// </value>
	public virtual string KeyPrefix { get; protected set; }

	/// <summary>
	/// 获取指定类型的缓存管理器实例。
	/// </summary>
	/// <typeparam name="TValue">要缓存的数据类型。</typeparam>
	/// <returns>用于管理 <typeparamref name="TValue"/> 类型缓存项的 <see cref="ICacheManager{T}"/> 实例。</returns>
	protected abstract ICacheManager<TValue> GetCacheManager<TValue>();

	/// <summary>
	/// 通过在键前添加配置的键前缀来重写指定的缓存键。
	/// </summary>
	/// <param name="key">要重写的原始缓存键。</param>
	/// <returns>
	/// 重写后的缓存键，格式为 "{KeyPrefix}.Cache.{key}"；如果未配置 <see cref="KeyPrefix"/>
	/// 或键已以此模式开头，则原样返回原始键。
	/// </returns>
	/// <remarks>
	/// 此方法确保所有缓存键遵循一致的命名约定。如果键已包含预期的前缀模式，则原样返回，
	/// 以避免重复添加前缀。比较不区分大小写。
	/// </remarks>
	protected virtual string RewriteKey(string key)
	{
		if (string.IsNullOrEmpty(KeyPrefix))
		{
			return key;
		}

		return key.StartsWith($"{KeyPrefix}.Cache.", StringComparison.OrdinalIgnoreCase) ? key : $"{KeyPrefix}.Cache.{key}";
	}

	/// <summary>
	/// 使用指定的键、值和可选的过期时间创建 <see cref="CacheItem{TValue}"/> 实例。
	/// </summary>
	/// <typeparam name="TValue">要缓存的值类型。</typeparam>
	/// <param name="key">缓存项的键标识符。</param>
	/// <param name="value">要存储在缓存中的值。</param>
	/// <param name="timeout">
	/// 指定缓存项绝对过期时间的可选 <see cref="TimeSpan"/>。
	/// 如果为 <see langword="null"/> 或小于等于 <see cref="TimeSpan.Zero"/>，则创建不带过期时间的缓存项。
	/// </param>
	/// <returns>
	/// 如果 <paramref name="timeout"/> 大于 <see cref="TimeSpan.Zero"/>，则返回配置了绝对过期的
	/// <see cref="CacheItem{TValue}"/>；否则返回不带过期设置的缓存项。
	/// </returns>
	/// <remarks>
	/// 此辅助方法统一了缓存项的创建，确保一致的过期行为。
	/// 当提供了正数的过期时间时，缓存项使用 <see cref="CacheExpirationMode.Absolute"/> 过期模式。
	/// </remarks>
	protected virtual CacheItem<TValue> GetCacheItem<TValue>(string key, TValue value, TimeSpan? timeout)
	{
		CacheItem<TValue> item;
		if (timeout > TimeSpan.Zero)
		{
			item = new CacheItem<TValue>(key, value, CacheExpirationMode.Absolute, timeout.Value);
		}
		else
		{
			item = new CacheItem<TValue>(key, value);
		}

		return item;
	}
}
