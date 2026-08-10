namespace Nerosoft.Euonia.Caching;

public partial class BaseCacheManager<TValue>
{
	/// <inheritdoc />
	public void Expire(string key, CacheExpirationMode mode, TimeSpan timeout)
		=> ExpireInternal(key, null, mode, timeout);

	/// <inheritdoc />
	public void Expire(string key, string region, CacheExpirationMode mode, TimeSpan timeout)
		=> ExpireInternal(key, region, mode, timeout);

	/// <summary>
	/// 根据指定的过期模式更新缓存项的过期时间，并写回缓存。
	/// </summary>
	/// <param name="key">缓存键。</param>
	/// <param name="region">缓存键所在的区域；可为 <c>null</c>。</param>
	/// <param name="mode">过期模式。</param>
	/// <param name="timeout">过期时间值。</param>
	private void ExpireInternal(string key, string region, CacheExpirationMode mode, TimeSpan timeout)
	{
		CheckDisposed();

		var item = GetCacheItemInternal(key, region);
		if (item == null)
		{
			return;
		}

		if (mode == CacheExpirationMode.Absolute)
		{
			item = item.WithAbsoluteExpiration(timeout);
		}
		else if (mode == CacheExpirationMode.Sliding)
		{
			item = item.WithSlidingExpiration(timeout);
		}
		else if (mode == CacheExpirationMode.None)
		{
			item = item.WithNoExpiration();
		}
		else if (mode == CacheExpirationMode.Default)
		{
			item = item.WithDefaultExpiration();
		}

		PutInternal(item);
	}

	/// <inheritdoc />
	public void Expire(string key, DateTimeOffset absoluteExpiration)
	{
		var timeout = absoluteExpiration.UtcDateTime - DateTime.UtcNow;
		if (timeout <= TimeSpan.Zero)
		{
			throw new ArgumentException(Resources.IDS_EXPIRATION_VALUE_MUST_BE_GREATER_THAN_ZERO, nameof(absoluteExpiration));
		}

		Expire(key, CacheExpirationMode.Absolute, timeout);
	}

	/// <inheritdoc />
	public void Expire(string key, string region, DateTimeOffset absoluteExpiration)
	{
		var timeout = absoluteExpiration.UtcDateTime - DateTime.UtcNow;
		if (timeout <= TimeSpan.Zero)
		{
			throw new ArgumentException(Resources.IDS_EXPIRATION_VALUE_MUST_BE_GREATER_THAN_ZERO, nameof(absoluteExpiration));
		}

		Expire(key, region, CacheExpirationMode.Absolute, timeout);
	}

	/// <inheritdoc />
	public void Expire(string key, TimeSpan slidingExpiration)
	{
		if (slidingExpiration <= TimeSpan.Zero)
		{
			throw new ArgumentException(Resources.IDS_EXPIRATION_VALUE_MUST_BE_GREATER_THAN_ZERO, nameof(slidingExpiration));
		}

		Expire(key, CacheExpirationMode.Sliding, slidingExpiration);
	}

	/// <inheritdoc />
	public void Expire(string key, string region, TimeSpan slidingExpiration)
	{
		if (slidingExpiration <= TimeSpan.Zero)
		{
			throw new ArgumentException(Resources.IDS_EXPIRATION_VALUE_MUST_BE_GREATER_THAN_ZERO, nameof(slidingExpiration));
		}

		Expire(key, region, CacheExpirationMode.Sliding, slidingExpiration);
	}

	/// <inheritdoc />
	public void RemoveExpiration(string key)
	{
		Expire(key, CacheExpirationMode.None, default);
	}

	/// <inheritdoc />
	public void RemoveExpiration(string key, string region)
	{
		Expire(key, region, CacheExpirationMode.None, default);
	}
}