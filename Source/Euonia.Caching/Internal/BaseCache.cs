using System.Globalization;

namespace Nerosoft.Euonia.Caching.Internal;

/// <summary>
/// BaseCache 类实现了此缓存库的整体逻辑，并将添加、获取或移除等具体实现委托给派生类。
/// <para>
/// 要使用此基类，只需重写 Add、Get、Put 和 Remove 的抽象方法。
/// <br/> <c>ICache</c> 定义的所有其他方法都将委托给这些方法。
/// </para>
/// </summary>
/// <typeparam name="TValue">缓存值的类型。</typeparam>
public abstract class BaseCache<TValue> : ICache<TValue>
{
	/// <summary>
	/// 初始化 <see cref="BaseCache{TCacheValue}"/> 类的新实例。
	/// </summary>
	protected internal BaseCache()
	{
	}

	/// <summary>
	/// 终结 <see cref="BaseCache{TCacheValue}"/> 类的实例。
	/// </summary>
	~BaseCache()
	{
		Dispose(false);
	}

	/// <summary>
	/// 获取或设置一个值，指示此 <see cref="BaseCache{TCacheValue}"/> 是否已释放。
	/// </summary>
	/// <value>如果已释放，则为 <c>true</c>；否则为 <c>false</c>。</value>
	protected bool Disposed { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示此 <see cref="BaseCache{TCacheValue}"/> 是否正在释放。
	/// </summary>
	/// <value>如果正在释放，则为 <c>true</c>；否则为 <c>false</c>。</value>
	protected bool Disposing { get; set; }

	/// <summary>
	/// 获取或设置指定键的值。此索引器与对应的 <see cref="Put(string, TValue)"/> 和 <see cref="Get(string)"/> 调用相同。
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <returns>给定 <paramref name="key"/> 存储在缓存中的值。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="key"/> 为 <c>null</c> 时抛出。</exception>
	public virtual TValue this[string key]
	{
		get => Get(key);
		set => Put(key, value);
	}

	/// <summary>
	/// 获取或设置指定键和区域的值。此索引器与对应的 <see cref="Put(string, TValue, string)"/> 和
	/// <see cref="Get(string, string)"/> 调用相同。
	/// <para>
	/// 指定 <paramref name="region"/> 后，该键将<b>不会</b>在全局缓存中找到。
	/// </para>
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns>
	/// 给定 <paramref name="key"/> 和 <paramref name="region"/> 存储在缓存中的值。
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="key"/> 或 <paramref name="region"/> 为 <c>null</c> 时抛出。
	/// </exception>
	public virtual TValue this[string key, string region]
	{
		get => Get(key, region);
		set => Put(key, value, region);
	}

	/// <summary>
	/// 向缓存中添加指定键的值。
	/// <para>
	/// 如果指定的 <paramref name="key"/> 已存在于缓存中，则 <c>Add</c> 方法将<b>不会</b>成功！
	/// </para>
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <param name="value">应被缓存的值。</param>
	/// <returns>
	/// 如果该键尚未添加到缓存中，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="key"/> 或 <paramref name="value"/> 为 <c>null</c> 时抛出。
	/// </exception>
	public virtual bool Add(string key, TValue value)
	{
		// 空值检查在项的构造函数中完成
		var item = new CacheItem<TValue>(key, value);
		return Add(item);
	}

	/// <summary>
	/// 向缓存中添加指定键和区域的值。
	/// <para>
	/// 如果指定的 <paramref name="key"/> 已存在于缓存中，则 <c>Add</c> 方法将<b>不会</b>成功！
	/// </para>
	/// <para>
	/// 指定 <paramref name="region"/> 后，该键将<b>不会</b>在全局缓存中找到。
	/// </para>
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <param name="value">应被缓存的值。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns>
	/// 如果该键尚未添加到缓存中，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="key"/>、<paramref name="value"/> 或 <paramref name="region"/> 为 <c>null</c> 时抛出。
	/// </exception>
	public virtual bool Add(string key, TValue value, string region)
	{
		// 空值检查在项的构造函数中完成
		var item = new CacheItem<TValue>(key, region, value);
		return Add(item);
	}

	/// <summary>
	/// 将指定的 <c>CacheItem</c> 添加到缓存中。
	/// <para>
	/// 使用此重载可以覆盖缓存配置的过期设置，仅为该 <paramref name="item"/> 定义自定义过期时间。
	/// </para>
	/// <para>
	/// 如果指定的 <paramref name="item"/> 已存在于缓存中，则 <c>Add</c> 方法将<b>不会</b>成功！
	/// </para>
	/// </summary>
	/// <param name="item">要添加到缓存中的 <c>CacheItem</c>。</param>
	/// <returns>
	/// 如果该键尚未添加到缓存中，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="item"/> 或其键或值为 <c>null</c> 时抛出。
	/// </exception>
	public virtual bool Add(CacheItem<TValue> item)
	{
		Check.EnsureNotNull(item, nameof(item));

		return AddInternal(item);
	}

	/// <summary>
	/// 清空此缓存，移除基础缓存及所有区域中的所有项。
	/// </summary>
	public abstract void Clear();

	/// <summary>
	/// 清空缓存区域，仅移除指定 <paramref name="region"/> 中的所有项。
	/// </summary>
	/// <param name="region">缓存区域。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="region"/> 为 <c>null</c> 时抛出。</exception>
	public abstract void ClearRegion(string region);

	/// <summary>
	/// 执行与释放、重置非托管资源相关的应用程序定义任务。
	/// </summary>
	public void Dispose()
	{
		Dispose(true);
		GC.SuppressFinalize(this);
	}

	/// <inheritdoc />
	public abstract bool Exists(string key);

	/// <inheritdoc />
	public abstract bool Exists(string key, string region);

	/// <summary>
	/// 获取指定键的值。
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <returns>给定 <paramref name="key"/> 存储在缓存中的值。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="key"/> 为 <c>null</c> 时抛出。</exception>
	public virtual TValue Get(string key)
	{
		var item = GetCacheItem(key);

		if (item != null && item.Key.Equals(key))
		{
			return item.Value;
		}

		return default;
	}

	/// <summary>
	/// 获取指定键和区域的值。
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns>
	/// 给定 <paramref name="key"/> 和 <paramref name="region"/> 存储在缓存中的值。
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="key"/> 或 <paramref name="region"/> 为 <c>null</c> 时抛出。
	/// </exception>
	public virtual TValue Get(string key, string region)
	{
		var item = GetCacheItem(key, region);

		if (item != null && item.Key.Equals(key) && item.Region != null && item.Region.Equals(region))
		{
			return item.Value;
		}

		return default;
	}

	/// <summary>
	/// 获取指定键的值，并将其转换为指定类型。
	/// </summary>
	/// <typeparam name="TOut">值被转换并返回的类型。</typeparam>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <returns>给定 <paramref name="key"/> 存储在缓存中的值。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="key"/> 为 <c>null</c> 时抛出。</exception>
	/// <exception cref="InvalidCastException">
	/// 如果未定义从 <c>TCacheValue</c> 到 <c>TOut</c> 的显式转换。
	/// </exception>
	public virtual TOut Get<TOut>(string key)
	{
		object value = Get(key);
		return GetCasted<TOut>(value);
	}

	/// <summary>
	/// 获取指定键和区域的值，并将其转换为指定类型。
	/// </summary>
	/// <typeparam name="TOut">缓存值应转换成的类型。</typeparam>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns>
	/// 给定 <paramref name="key"/> 和 <paramref name="region"/> 存储在缓存中的值。
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="key"/> 或 <paramref name="region"/> 为 <c>null</c> 时抛出。
	/// </exception>
	/// <exception cref="InvalidCastException">
	/// 如果未定义从 <c>TCacheValue</c> 到 <c>TOut</c> 的显式转换。
	/// </exception>
	public virtual TOut Get<TOut>(string key, string region)
	{
		object value = Get(key, region);
		return GetCasted<TOut>(value);
	}

	/// <summary>
	/// 获取指定键的 <c>CacheItem</c>。
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <returns><c>CacheItem</c>。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="key"/> 为 <c>null</c> 时抛出。</exception>
	public virtual CacheItem<TValue> GetCacheItem(string key)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

		return GetCacheItemInternal(key);
	}

	/// <summary>
	/// 获取指定键和区域的 <c>CacheItem</c>。
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns><c>CacheItem</c>。</returns>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="key"/> 或 <paramref name="region"/> 为 <c>null</c> 时抛出。
	/// </exception>
	public virtual CacheItem<TValue> GetCacheItem(string key, string region)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
		Check.EnsureNotNullOrWhiteSpace(region, nameof(region));

		return GetCacheItemInternal(key, region);
	}

	/// <summary>
	/// 将指定键的值放入缓存。
	/// <para>
	/// 如果 <paramref name="key"/> 已存在于缓存中，则现有值将被新的 <paramref name="value"/> 替换。
	/// </para>
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <param name="value">应被缓存的值。</param>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="key"/> 或 <paramref name="value"/> 为 <c>null</c> 时抛出。
	/// </exception>
	public virtual void Put(string key, TValue value)
	{
		var item = new CacheItem<TValue>(key, value);
		Put(item);
	}

	/// <summary>
	/// 将指定键和区域的值放入缓存。
	/// <para>
	/// 如果 <paramref name="key"/> 已存在于缓存中，则现有值将被新的 <paramref name="value"/> 替换。
	/// </para>
	/// <para>
	/// 指定 <paramref name="region"/> 后，该键将<b>不会</b>在全局缓存中找到。
	/// </para>
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <param name="value">应被缓存的值。</param>
	/// <param name="region">缓存区域。</param>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="key"/>、<paramref name="value"/> 或 <paramref name="region"/> 为 <c>null</c> 时抛出。
	/// </exception>
	public virtual void Put(string key, TValue value, string region)
	{
		var item = new CacheItem<TValue>(key, region, value);
		Put(item);
	}

	/// <summary>
	/// 将指定的 <c>CacheItem</c> 放入缓存。
	/// <para>
	/// 如果 <paramref name="item"/> 已存在于缓存中，则现有项将被新的 <paramref name="item"/> 替换。
	/// </para>
	/// <para>
	/// 使用此重载可以覆盖缓存配置的过期设置，仅为该 <paramref name="item"/> 定义自定义过期时间。
	/// </para>
	/// </summary>
	/// <param name="item">要缓存的 <c>CacheItem</c>。</param>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="item"/> 或其键或值为 <c>null</c> 时抛出。
	/// </exception>
	public virtual void Put(CacheItem<TValue> item)
	{
		Check.EnsureNotNull(item, nameof(item));

		PutInternal(item);
	}

	/// <summary>
	/// 从缓存中移除指定键的值。
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <returns>
	/// 如果找到并从缓存中移除了该键，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="key"/> 为 <c>null</c> 时抛出。</exception>
	public virtual bool Remove(string key)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));

		return RemoveInternal(key);
	}

	/// <summary>
	/// 从缓存中移除指定键和区域的值。
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns>
	/// 如果找到并从缓存中移除了该键，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	/// <exception cref="ArgumentNullException">
	/// 当 <paramref name="key"/> 或 <paramref name="region"/> 为 <c>null</c> 时抛出。
	/// </exception>
	public virtual bool Remove(string key, string region)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
		Check.EnsureNotNullOrWhiteSpace(region, nameof(region));

		return RemoveInternal(key, region);
	}

	/// <summary>
	/// 向缓存中添加值。
	/// </summary>
	/// <param name="item">要添加到缓存中的 <c>CacheItem</c>。</param>
	/// <returns>
	/// 如果该键尚未添加到缓存中，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	protected abstract bool AddInternal(CacheItem<TValue> item);

	/// <summary>
	/// 将值放入缓存。
	/// </summary>
	/// <param name="item">要添加到缓存中的 <c>CacheItem</c>。</param>
	protected abstract void PutInternal(CacheItem<TValue> item);

	/// <summary>
	/// 释放非托管资源，并可选择性地释放托管资源。
	/// </summary>
	/// <param name="disposeManaged">
	/// <c>true</c> 表示同时释放托管和非托管资源；<c>false</c> 表示仅释放非托管资源。
	/// </param>
	protected virtual void Dispose(bool disposeManaged)
	{
		Disposing = true;
		if (!Disposed)
		{
			if (disposeManaged)
			{
				// 不执行任何操作
			}

			Disposed = true;
		}

		Disposing = false;
	}

	/// <summary>
	/// 获取指定键的 <c>CacheItem</c>。
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <returns><c>CacheItem</c>。</returns>
	protected abstract CacheItem<TValue> GetCacheItemInternal(string key);

	/// <summary>
	/// 获取指定键和区域的 <c>CacheItem</c>。
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns><c>CacheItem</c>。</returns>
	protected abstract CacheItem<TValue> GetCacheItemInternal(string key, string region);

	/// <summary>
	/// 从缓存中移除指定键的值。
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <returns>
	/// 如果找到并从缓存中移除了该键，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	protected abstract bool RemoveInternal(string key);

	/// <summary>
	/// 从缓存中移除指定键和区域的值。
	/// </summary>
	/// <param name="key">用于标识缓存中项的键。</param>
	/// <param name="region">缓存区域。</param>
	/// <returns>
	/// 如果找到并从缓存中移除了该键，则为 <c>true</c>；否则为 <c>false</c>。
	/// </returns>
	protected abstract bool RemoveInternal(string key, string region);

	/// <summary>
	/// 检查实例是否已释放。
	/// </summary>
	/// <exception cref="ObjectDisposedException">如果实例已释放。</exception>
	protected void CheckDisposed()
	{
		#if NET8_0_OR_GREATER
		ObjectDisposedException.ThrowIf(Disposed, this);
		#else
		if(Disposed)
		{
			throw new ObjectDisposedException(GetType().FullName);
		}
		#endif
	}

	/// <summary>
	/// 将值转换为 <c>TOut</c>。
	/// </summary>
	/// <typeparam name="TOut">类型。</typeparam>
	/// <param name="value">值。</param>
	/// <returns>转换后的值。</returns>
	protected static TOut GetCasted<TOut>(object value)
	{
		if (value == null)
		{
			return default;
		}

		// 快速路径：类型已匹配时直接转换，避免 Convert.ChangeType 的开销。
		if (value is TOut direct)
		{
			return direct;
		}

		try
		{
			var changed = Convert.ChangeType(value, typeof(TOut), CultureInfo.InvariantCulture);
			return (TOut)changed;
		}
		catch
		{
			return (TOut)value;
		}
	}
}