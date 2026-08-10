using Nerosoft.Euonia.Caching.Internal;
using System.Runtime.Serialization;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 将存储在缓存中的缓存项，包含缓存值以及缓存句柄和管理器所需的附加信息。
/// </summary>
/// <typeparam name="T">缓存值的类型。</typeparam>
[Serializable]
public class CacheItem<T> : ISerializable, ICacheItemProperties
{
	/// <summary>
	/// 初始化 <see cref="CacheItem{T}"/> 类的新实例。
	/// </summary>
	/// <param name="key">缓存键。</param>
	/// <param name="value">缓存值。</param>
	/// <exception cref="ArgumentNullException">当 <c>key</c> 或 <c>value</c> 为 <c>null</c> 时抛出。</exception>
	public CacheItem(string key, T value)
		: this(key, null, value, null, null, null)
	{
	}

	/// <summary>
	/// 初始化 <see cref="CacheItem{T}"/> 类的新实例。
	/// </summary>
	/// <param name="key">缓存键。</param>
	/// <param name="value">缓存值。</param>
	/// <param name="region">缓存区域。</param>
	/// <exception cref="ArgumentNullException">当 <c>key</c>、<c>value</c> 或 <c>region</c> 为 <c>null</c> 时抛出。</exception>
	public CacheItem(string key, string region, T value)
		: this(key, region, value, null, null, null)
	{
		Check.EnsureNotNullOrWhiteSpace(region, nameof(region));
	}

	/// <summary>
	/// 初始化 <see cref="CacheItem{T}"/> 类的新实例。
	/// </summary>
	/// <param name="key">缓存键。</param>
	/// <param name="value">缓存值。</param>
	/// <param name="expiration">过期模式。</param>
	/// <param name="timeout">过期时间。</param>
	/// <exception cref="ArgumentNullException">当 <c>key</c> 或 <c>value</c> 为 <c>null</c> 时抛出。</exception>
	public CacheItem(string key, T value, CacheExpirationMode expiration, TimeSpan timeout)
		: this(key, null, value, expiration, timeout, null, null, false)
	{
	}

	/// <summary>
	/// 初始化 <see cref="CacheItem{T}"/> 类的新实例。
	/// </summary>
	/// <param name="key">缓存键。</param>
	/// <param name="value">缓存值。</param>
	/// <param name="region">缓存区域。</param>
	/// <param name="expiration">过期模式。</param>
	/// <param name="timeout">过期时间。</param>
	/// <exception cref="ArgumentNullException">当 <c>key</c>、<c>value</c> 或 <c>region</c> 为 <c>null</c> 时抛出。</exception>
	public CacheItem(string key, string region, T value, CacheExpirationMode expiration, TimeSpan timeout)
		: this(key, region, value, expiration, timeout, null, null, false)
	{
		Check.EnsureNotNullOrWhiteSpace(region, nameof(region));
	}

	/// <summary>
	/// 初始化 <see cref="CacheItem{T}"/> 类的新实例。
	/// </summary>
	protected CacheItem()
	{
	}

	/// <summary>
	/// 初始化 <see cref="CacheItem{T}"/> 类的新实例。
	/// </summary>
	/// <param name="info">序列化信息。</param>
	/// <param name="context">序列化上下文。</param>
	/// <exception cref="ArgumentNullException">当 <c>info</c> 为 <c>null</c> 时抛出。</exception>
	protected CacheItem(SerializationInfo info, StreamingContext context)
	{
		Check.EnsureNotNull(info, nameof(info));

		Key = info.GetString(nameof(Key));
		Value = (T)info.GetValue(nameof(Value), typeof(T));
		ValueType = (Type)info.GetValue(nameof(ValueType), typeof(Type));
		Region = info.GetString(nameof(Region));
		ExpirationMode = (CacheExpirationMode)info.GetValue(nameof(ExpirationMode), typeof(CacheExpirationMode))!;
		ExpirationTimeout = (TimeSpan)info.GetValue(nameof(ExpirationTimeout), typeof(TimeSpan))!;
		CreatedUtc = info.GetDateTime(nameof(CreatedUtc));
		LastAccessedUtc = info.GetDateTime(nameof(LastAccessedUtc));
		UsesExpirationDefaults = info.GetBoolean(nameof(UsesExpirationDefaults));
	}

	private CacheItem(string key, string region, T value, CacheExpirationMode? expiration, TimeSpan? timeout, DateTime? created, DateTime? lastAccessed = null, bool expirationDefaults = true)
	{
		Check.EnsureNotNullOrWhiteSpace(key, nameof(key));
		Check.EnsureNotNull(value, nameof(value));

		Key = key;
		Region = region;
		Value = value;
		ValueType = value.GetType();
		ExpirationMode = expiration ?? CacheExpirationMode.Default;
		ExpirationTimeout = ExpirationMode is CacheExpirationMode.None or CacheExpirationMode.Default ? TimeSpan.Zero : timeout ?? TimeSpan.Zero;
		UsesExpirationDefaults = expirationDefaults;

		// 对过大的过期时间进行校验。
		// 否则会导致各种错误（例如在使用 long.MaxValue ticks 的 TimeSpan 时向滑动过期添加时间）
		if (ExpirationTimeout.TotalDays > 365)
		{
			throw new ArgumentOutOfRangeException(nameof(timeout), string.Format(Resources.IDS_EXPIRATION_TIMEOUT_MUST_BE_BETWEEN, "00:00:00 ", " 365:00:00:00"));
		}

		if (ExpirationMode != CacheExpirationMode.Default && ExpirationMode != CacheExpirationMode.None && ExpirationTimeout <= TimeSpan.Zero)
		{
			throw new ArgumentOutOfRangeException(nameof(timeout), Resources.IDS_EXPIRATION_TIMEOUT_MUST_BE_GREATER_THAN_ZERO_IF_EXPIRATION_MODE_IS_DEFINED);
		}

		if (created.HasValue && created.Value.Kind != DateTimeKind.Utc)
		{
			throw new ArgumentException(string.Format(Resources.IDS_DATE_KIND_OF_PARAMETER_MUST_BE, nameof(created), DateTimeKind.Utc), nameof(created));
		}

		if (lastAccessed.HasValue && lastAccessed.Value.Kind != DateTimeKind.Utc)
		{
			throw new ArgumentException(string.Format(Resources.IDS_DATE_KIND_OF_PARAMETER_MUST_BE, nameof(lastAccessed), DateTimeKind.Utc), nameof(lastAccessed));
		}

		CreatedUtc = created ?? DateTime.UtcNow;
		LastAccessedUtc = lastAccessed ?? DateTime.UtcNow;
	}

	/// <summary>
	/// 获取一个值，指示该项在逻辑上是否已过期。
	/// 根据缓存供应商的不同，该项可能仍存在于缓存中，尽管根据过期模式和超时时间，该项已过期。
	/// </summary>
	public bool IsExpired
	{
		get
		{
			var now = DateTime.UtcNow;
			if (ExpirationMode == CacheExpirationMode.Absolute
			    && CreatedUtc.Add(ExpirationTimeout) < now)
			{
				return true;
			}
			else if (ExpirationMode == CacheExpirationMode.Sliding
			         && LastAccessedUtc.Add(ExpirationTimeout) < now)
			{
				return true;
			}

			return false;
		}
	}

	/// <summary>
	/// 获取缓存项的创建日期。
	/// </summary>
	/// <value>创建日期。</value>
	public DateTime CreatedUtc { get; }

	/// <summary>
	/// 获取过期模式。
	/// </summary>
	/// <value>过期模式。</value>
	public CacheExpirationMode ExpirationMode { get; }

	/// <summary>
	/// 获取过期时间。
	/// </summary>
	/// <value>过期时间。</value>
	public TimeSpan ExpirationTimeout { get; }

	/// <summary>
	/// 获取缓存键。
	/// </summary>
	/// <value>缓存键。</value>
	public string Key { get; }

	/// <summary>
	/// 获取或设置缓存项的最后访问日期。
	/// </summary>
	/// <value>最后访问日期。</value>
	public DateTime LastAccessedUtc { get; set; }

	/// <summary>
	/// 获取缓存区域。
	/// </summary>
	/// <value>缓存区域。</value>
	public string Region { get; }

	/// <summary>
	/// 获取缓存值。
	/// </summary>
	/// <value>缓存值。</value>
	public T Value { get; }

	/// <summary>
	/// 获取缓存值的类型。
	/// <para>此类型可能用于序列化和反序列化。</para>
	/// </summary>
	/// <value>缓存值的类型。</value>
	public Type ValueType { get; }

	/// <summary>
	/// 获取一个值，指示缓存项是否使用缓存句柄配置的过期时间。
	/// </summary>
	public bool UsesExpirationDefaults { get; } = true;

	/// <summary>
	/// 使用序列化目标对象所需的数据填充 <see cref="T:System.Runtime.Serialization.SerializationInfo"/>。
	/// </summary>
	/// <param name="info">
	/// 要填充数据的 <see cref="T:System.Runtime.Serialization.SerializationInfo"/>。
	/// </param>
	/// <param name="context">
	/// 此序列化的目标（参见 <see cref="T:System.Runtime.Serialization.StreamingContext"/>）。
	/// </param>
	/// <exception cref="ArgumentNullException">当 <c>info</c> 为 <c>null</c> 时抛出。</exception>
	public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
	{
		Check.EnsureNotNull(info, nameof(info));

		info.AddValue(nameof(Key), Key);
		info.AddValue(nameof(Value), Value);
		info.AddValue(nameof(ValueType), ValueType);
		info.AddValue(nameof(Region), Region);
		info.AddValue(nameof(ExpirationMode), ExpirationMode);
		info.AddValue(nameof(ExpirationTimeout), ExpirationTimeout);
		info.AddValue(nameof(CreatedUtc), CreatedUtc);
		info.AddValue(nameof(LastAccessedUtc), LastAccessedUtc);
		info.AddValue(nameof(UsesExpirationDefaults), UsesExpirationDefaults);
	}

	/// <inheritdoc />
	public override string ToString()
	{
		return !string.IsNullOrWhiteSpace(Region)
			? $"'{Region}:{Key}', exp:{ExpirationMode} {ExpirationTimeout}, lastAccess:{LastAccessedUtc}"
			: $"'{Key}', exp:{ExpirationMode} {ExpirationTimeout}, lastAccess:{LastAccessedUtc}";
	}

	internal CacheItem<T> WithExpiration(CacheExpirationMode mode, TimeSpan timeout, bool usesHandleDefault = true) =>
		new(Key, Region, Value, mode, timeout, mode == CacheExpirationMode.Absolute ? DateTime.UtcNow : CreatedUtc, LastAccessedUtc, usesHandleDefault);

	/// <summary>
	/// 创建当前缓存项的副本，并设置新的绝对过期时间。
	/// 此方法不会更改缓存中该项的状态。使用 <c>Put</c> 或类似方法，用返回的副本更新缓存。
	/// </summary>
	/// <remarks>我们不会克隆缓存项或值。</remarks>
	/// <param name="absoluteExpiration">绝对过期日期。</param>
	/// <returns>缓存项的新实例。</returns>
	public CacheItem<T> WithAbsoluteExpiration(DateTimeOffset absoluteExpiration)
	{
		var timeout = absoluteExpiration - DateTimeOffset.UtcNow;
		if (timeout <= TimeSpan.Zero)
		{
			throw new ArgumentException(Resources.IDS_EXPIRATION_VALUE_MUST_BE_GREATER_THAN_ZERO, nameof(absoluteExpiration));
		}

		return WithExpiration(CacheExpirationMode.Absolute, timeout, false);
	}

	/// <summary>
	/// 创建当前缓存项的副本，并设置新的绝对过期时间。
	/// 此方法不会更改缓存中该项的状态。使用 <c>Put</c> 或类似方法，用返回的副本更新缓存。
	/// </summary>
	/// <remarks>我们不会克隆缓存项或值。</remarks>
	/// <param name="absoluteExpiration">绝对过期日期。</param>
	/// <returns>缓存项的新实例。</returns>
	public CacheItem<T> WithAbsoluteExpiration(TimeSpan absoluteExpiration)
	{
		if (absoluteExpiration <= TimeSpan.Zero)
		{
			throw new ArgumentException(Resources.IDS_EXPIRATION_VALUE_MUST_BE_GREATER_THAN_ZERO, nameof(absoluteExpiration));
		}

		return WithExpiration(CacheExpirationMode.Absolute, absoluteExpiration, false);
	}

	/// <summary>
	/// 创建当前缓存项的副本，并设置新的滑动过期时间。
	/// 此方法不会更改缓存中该项的状态。使用 <c>Put</c> 或类似方法，用返回的副本更新缓存。
	/// </summary>
	/// <remarks>我们不会克隆缓存项或值。</remarks>
	/// <param name="slidingExpiration">滑动过期时间。</param>
	/// <returns>缓存项的新实例。</returns>
	public CacheItem<T> WithSlidingExpiration(TimeSpan slidingExpiration)
	{
		if (slidingExpiration <= TimeSpan.Zero)
		{
			throw new ArgumentException(Resources.IDS_EXPIRATION_VALUE_MUST_BE_GREATER_THAN_ZERO, nameof(slidingExpiration));
		}

		return WithExpiration(CacheExpirationMode.Sliding, slidingExpiration, false);
	}

	/// <summary>
	/// 创建不带过期时间的当前缓存项副本。可用于更新缓存并移除该项之前配置的任何过期时间。
	/// 此方法不会更改缓存中该项的状态。使用 <c>Put</c> 或类似方法，用返回的副本更新缓存。
	/// </summary>
	/// <remarks>我们不会克隆缓存项或值。</remarks>
	/// <returns>缓存项的新实例。</returns>
	public CacheItem<T> WithNoExpiration() =>
		new(Key, Region, Value, CacheExpirationMode.None, TimeSpan.Zero, CreatedUtc, LastAccessedUtc, false);

	/// <summary>
	/// 创建不带显式过期时间的当前缓存项副本，指示缓存使用缓存句柄配置中定义的默认值。
	/// 此方法不会更改缓存中该项的状态。使用 <c>Put</c> 或类似方法，用返回的副本更新缓存。
	/// </summary>
	/// <remarks>我们不会克隆缓存项或值。</remarks>
	/// <returns>缓存项的新实例。</returns>
	public CacheItem<T> WithDefaultExpiration() =>
		new(Key, Region, Value, CacheExpirationMode.Default, TimeSpan.Zero, CreatedUtc, LastAccessedUtc);

	/// <summary>
	/// 创建带新值的当前缓存项副本。
	/// 此方法不会更改缓存中该项的状态。使用 <c>Put</c> 或类似方法，用返回的副本更新缓存。
	/// </summary>
	/// <remarks>我们不会克隆缓存项或值。</remarks>
	/// <param name="value">新值。</param>
	/// <returns>缓存项的新实例。</returns>
	public CacheItem<T> WithValue(T value) =>
		new(Key, Region, value, ExpirationMode, ExpirationTimeout, CreatedUtc, LastAccessedUtc, UsesExpirationDefaults);

	/// <summary>
	/// 创建带指定创建日期的当前缓存项副本。
	/// 此方法不会更改缓存中该项的状态。使用 <c>Put</c> 或类似方法，用返回的副本更新缓存。
	/// </summary>
	/// <remarks>我们不会克隆缓存项或值。</remarks>
	/// <param name="created">新的创建日期。</param>
	/// <returns>缓存项的新实例。</returns>
	public CacheItem<T> WithCreated(DateTime created) =>
		new(Key, Region, Value, ExpirationMode, ExpirationTimeout, created, LastAccessedUtc, UsesExpirationDefaults);
}