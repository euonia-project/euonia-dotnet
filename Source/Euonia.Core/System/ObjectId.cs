/// <summary>
/// 对象标识符。
/// </summary>
public readonly struct ObjectId
{
	/// <summary>
	/// 使用 <see cref="int"/> 值创建新的 <see cref="ObjectId"/>。
	/// </summary>
	/// <param name="value">整数值。</param>
	public ObjectId(int value)
	{
		Value = value;
	}

	/// <summary>
	/// 使用 <see cref="long"/> 值创建新的 <see cref="ObjectId"/>。
	/// </summary>
	/// <param name="value">长整数值。</param>
	public ObjectId(long value)
	{
		Value = value;
	}

	/// <summary>
	/// 使用 <see cref="System.Guid"/> 值创建新的 <see cref="ObjectId"/>。
	/// </summary>
	/// <param name="value">Guid 值。</param>
	public ObjectId(Guid value)
	{
		Value = value;
	}

	/// <summary>
	/// 使用 <see cref="string"/> 值创建新的 <see cref="ObjectId"/>。
	/// </summary>
	/// <param name="value">字符串值。</param>
	public ObjectId(string value)
	{
		Value = value;
	}

	/// <summary>
	/// 获取标识符的实际值。
	/// </summary>
	public object Value { get; }

	/// <summary>
	/// 返回一个值，指示两个指定的 <see cref="ObjectId"/> 值是否相等。
	/// </summary>
	/// <param name="id1">第一个对象标识符。</param>
	/// <param name="id2">第二个对象标识符。</param>
	/// <returns>如果值相等则为 true；否则为 false。</returns>
	public static bool operator ==(ObjectId id1, ObjectId id2) => EqualityComparer<object>.Default.Equals(id1.Value, id2.Value);

	/// <summary>
	/// 返回一个值，指示两个指定的 <see cref="ObjectId"/> 值是否不相等。
	/// </summary>
	/// <param name="id1">第一个对象标识符。</param>
	/// <param name="id2">第二个对象标识符。</param>
	/// <returns>如果值不相等则为 true；否则为 false。</returns>
	public static bool operator !=(ObjectId id1, ObjectId id2) => !EqualityComparer<object>.Default.Equals(id1.Value, id2.Value);

	/// <summary>
	/// 定义将 <see cref="long"/> 隐式转换为 <see cref="ObjectId"/>。
	/// </summary>
	/// <param name="id">要转换的长整数值。</param>
	/// <returns>转换后的 <see cref="ObjectId"/>。</returns>
	public static implicit operator ObjectId(long id)
	{
		return new ObjectId(id);
	}

	/// <summary>
	/// 定义将 <see cref="ObjectId"/> 显式转换为 <see cref="long"/>。
	/// </summary>
	/// <param name="id">要转换的对象标识符。</param>
	/// <returns>转换后的长整数值。</returns>
	public static implicit operator long(ObjectId id)
	{
		return (long)id.Value;
	}

	/// <summary>
	/// 定义将 <see cref="int"/> 隐式转换为 <see cref="ObjectId"/>。
	/// </summary>
	/// <param name="id">要转换的整数值。</param>
	/// <returns>转换后的 <see cref="ObjectId"/>。</returns>
	public static implicit operator ObjectId(int id)
	{
		return new ObjectId(id);
	}

	/// <summary>
	/// 定义将 <see cref="ObjectId"/> 显式转换为 <see cref="int"/>。
	/// </summary>
	/// <param name="id">要转换的对象标识符。</param>
	/// <returns>转换后的整数值。</returns>
	public static implicit operator int(ObjectId id)
	{
		return (int)id.Value;
	}

	/// <summary>
	/// 定义将 <see cref="string"/> 隐式转换为 <see cref="ObjectId"/>。
	/// </summary>
	/// <param name="id">要转换的字符串值。</param>
	/// <returns>转换后的 <see cref="ObjectId"/>。</returns>
	public static implicit operator ObjectId(string id)
	{
		return new ObjectId(id);
	}

	/// <summary>
	/// 定义将 <see cref="ObjectId"/> 显式转换为 <see cref="string"/>。
	/// </summary>
	/// <param name="id">要转换的对象标识符。</param>
	/// <returns>转换后的字符串值。</returns>
	public static implicit operator string(ObjectId id)
	{
		return (string)id.Value;
	}

	/// <summary>
	/// 定义将 <see cref="System.Guid"/> 隐式转换为 <see cref="ObjectId"/>。
	/// </summary>
	/// <param name="id">要转换的 Guid 值。</param>
	/// <returns>转换后的 <see cref="ObjectId"/>。</returns>
	public static implicit operator ObjectId(Guid id)
	{
		return new ObjectId(id);
	}

	/// <summary>
	/// 定义将 <see cref="ObjectId"/> 显式转换为 <see cref="System.Guid"/>。
	/// </summary>
	/// <param name="id">要转换的对象标识符。</param>
	/// <returns>转换后的 Guid 值。</returns>
	public static implicit operator Guid(ObjectId id)
	{
		return (Guid)id.Value;
	}

	/// <summary>
	/// 使用雪花 ID 创建新的 <see cref="ObjectId"/> 实例。
	/// </summary>
	/// <returns>新的 <see cref="ObjectId"/> 实例。</returns>
	public static ObjectId Snowflake()
	{
		return new ObjectId(NewSnowflake());
	}

	/// <summary>
	/// 使用 <see cref="System.Guid"/> 创建新的 <see cref="ObjectId"/> 实例。
	/// </summary>
	/// <param name="type">GUID 类型。</param>
	/// <returns>新的 <see cref="ObjectId"/> 实例。</returns>
	public static ObjectId Guid(GuidType type)
	{
		return new ObjectId(NewGuid(type));
	}

	/// <summary>
	/// 使用随机字符串值创建新的 <see cref="ObjectId"/> 实例。
	/// </summary>
	/// <returns>新的 <see cref="ObjectId"/> 实例。</returns>
	public static ObjectId Random()
	{
		return new ObjectId(NewRandomId(DateTime.UtcNow.Ticks));
	}

	/// <summary>
	/// 使用 ULID（通用唯一字典排序标识符）创建新的 <see cref="ObjectId"/> 实例。
	/// </summary>
	/// <returns>新的 <see cref="ObjectId"/> 实例。</returns>
	public static ObjectId Ulid()
	{
		return new ObjectId(NewUlid());
	}

	/// <summary>
	/// 生成新的雪花 ID。
	/// </summary>
	/// <returns>新的雪花 ID。</returns>
	public static long NewSnowflake()
	{
		return SnowflakeId.Instance.Next();
	}

	/// <summary>
	/// 使用指定的 <see cref="GuidType"/> 生成新的 <see cref="System.Guid"/>。
	/// </summary>
	/// <param name="type">GUID 类型。</param>
	/// <returns>生成的 <see cref="System.Guid"/>。</returns>
	public static Guid NewGuid(GuidType type)
	{
		return GuidGenerator.Generate(type);
	}

	/// <summary>
	/// 生成新的 ULID（通用唯一字典排序标识符）。
	/// </summary>
	/// <returns>新生成的 ULID 字符串。</returns>
	public static string NewUlid()
	{
		return UlidGenerator.Generate();
	}

	/// <summary>
	/// 生成新的随机字符串 ID。
	/// </summary>
	/// <param name="seed">随机数种子。</param>
	/// <returns>生成的随机字符串 ID。</returns>
	public static string NewRandomId(long seed)
	{
		return RandomId.Generate(seed);
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return HashCode.Combine(Value);
	}

	/// <inheritdoc/>
	public override bool Equals(object obj)
	{
		if (obj is not ObjectId id)
		{
			return false;
		}

		return id.Value.Equals(Value);
	}
}

/// <summary>
/// 具有类型为 <typeparamref name="T"/> 的值的对象标识符。
/// </summary>
/// <typeparam name="T">值的类型。</typeparam>
public readonly struct ObjectId<T>
	where T : IEquatable<T>
{
	/// <summary>
	/// 创建新的 <see cref="ObjectId{T}"/> 实例。
	/// </summary>
	/// <param name="value">标识符值。</param>
	public ObjectId(T value)
	{
		Value = value;
	}

	/// <summary>
	/// 获取标识符的实际值。
	/// </summary>
	public T Value { get; }

	/// <summary>
	/// 返回一个值，指示当前实例与指定的 <see cref="ObjectId{T}"/> 是否具有相同的值。
	/// </summary>
	/// <param name="id1">第一个对象标识符。</param>
	/// <param name="id2">第二个对象标识符。</param>
	/// <returns>如果值相等则为 true；否则为 false。</returns>
	public static bool operator ==(ObjectId<T> id1, ObjectId<T> id2) => EqualityComparer<T>.Default.Equals(id1.Value, id2.Value);

	/// <summary>
	/// 返回一个值，指示当前实例与指定的 <see cref="ObjectId{T}"/> 是否具有不同的值。
	/// </summary>
	/// <param name="id1">第一个对象标识符。</param>
	/// <param name="id2">第二个对象标识符。</param>
	/// <returns>如果值不相等则为 true；否则为 false。</returns>
	public static bool operator !=(ObjectId<T> id1, ObjectId<T> id2) => !EqualityComparer<T>.Default.Equals(id1.Value, id2.Value);

	/// <summary>
	/// 定义将 <typeparamref name="T"/> 隐式转换为 <see cref="ObjectId{T}"/>。
	/// </summary>
	/// <param name="id">要转换的值。</param>
	/// <returns>转换后的 <see cref="ObjectId{T}"/>。</returns>
	public static implicit operator ObjectId<T>(T id)
	{
		return new ObjectId<T>(id);
	}

	/// <summary>
	/// 定义将 <see cref="ObjectId{T}"/> 显式转换为 <typeparamref name="T"/>。
	/// </summary>
	/// <param name="id">要转换的对象标识符。</param>
	/// <returns>转换后的值。</returns>
	public static implicit operator T(ObjectId<T> id)
	{
		return id.Value;
	}

	/// <inheritdoc/>
	public override bool Equals(object obj)
	{
		return obj is ObjectId<T> id &&
		       EqualityComparer<T>.Default.Equals(Value, id.Value);
	}

	/// <inheritdoc/>
	public override int GetHashCode()
	{
		return HashCode.Combine(Value);
	}
}