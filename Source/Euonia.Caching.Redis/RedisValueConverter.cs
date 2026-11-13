using System.Text.Json;
using Nerosoft.Euonia.Caching.Internal;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching.Redis;

/// <summary>
/// 非泛型的 Redis 值转换契约：在运行时按类型名序列化与反序列化缓存值。
/// </summary>
internal interface IRedisValueConverter
{
	/// <summary>
	/// 把指定值序列化为 Redis 可存储的表示。
	/// </summary>
	/// <typeparam name="T">值的类型。</typeparam>
	/// <param name="value">要序列化的值。</param>
	/// <returns>序列化后的 Redis 值。</returns>
	RedisValue ToRedisValue<T>(T value);

	/// <summary>
	/// 按目标类型名把 Redis 值反序列化为对象。
	/// </summary>
	/// <typeparam name="T">期望的返回类型。</typeparam>
	/// <param name="value">Redis 中存储的值。</param>
	/// <param name="valueType">原始值类型的名称，用于还原具体类型。</param>
	/// <returns>反序列化后的值。</returns>
	T FromRedisValue<T>(RedisValue value, string valueType);
}

/// <summary>
/// 针对单一值类型的 Redis 值转换契约，避免装箱与运行时类型判断。
/// </summary>
/// <typeparam name="T">要转换的值类型。</typeparam>
internal interface IRedisValueConverter<T>
{
	/// <summary>
	/// 把指定值转换为 Redis 可存储的表示。
	/// </summary>
	/// <param name="value">要转换的值。</param>
	/// <returns>转换后的 Redis 值。</returns>
	RedisValue ToRedisValue(T value);

	/// <summary>
	/// 把 Redis 值还原为 <typeparamref name="T"/> 类型的值。
	/// </summary>
	/// <param name="value">Redis 中存储的值。</param>
	/// <param name="valueType">原始值类型的名称，保留以兼容非泛型契约。</param>
	/// <returns>还原后的值。</returns>
	T FromRedisValue(RedisValue value, string valueType);
}

/// <summary>
/// 默认的 Redis 值转换实现：原生支持常用基元类型，其余类型使用 JSON 序列化。
/// </summary>
internal class RedisValueConverter : IRedisValueConverter,
                                     IRedisValueConverter<byte[]>,
                                     IRedisValueConverter<string>,
                                     IRedisValueConverter<int>,
                                     IRedisValueConverter<uint>,
                                     IRedisValueConverter<short>,
                                     IRedisValueConverter<float>,
                                     IRedisValueConverter<double>,
                                     IRedisValueConverter<bool>,
                                     IRedisValueConverter<long>,
                                     IRedisValueConverter<ulong>,
                                     IRedisValueConverter<object>
{
	private static readonly Type _byteArrayType = typeof(byte[]);
	private static readonly Type _stringType = typeof(string);
	private static readonly Type _intType = typeof(int);
	private static readonly Type _uIntType = typeof(uint);
	private static readonly Type _shortType = typeof(short);
	private static readonly Type _singleType = typeof(float);
	private static readonly Type _doubleType = typeof(double);
	private static readonly Type _boolType = typeof(bool);
	private static readonly Type _longType = typeof(long);
	private static readonly Type _uLongType = typeof(ulong);

	RedisValue IRedisValueConverter<byte[]>.ToRedisValue(byte[] value) => value;

	byte[] IRedisValueConverter<byte[]>.FromRedisValue(RedisValue value, string valueType) => ((byte[]?)value)!;

	RedisValue IRedisValueConverter<string>.ToRedisValue(string value) => value;

	string IRedisValueConverter<string>.FromRedisValue(RedisValue value, string valueType) => ((string?)value)!;

	RedisValue IRedisValueConverter<int>.ToRedisValue(int value) => value;

	int IRedisValueConverter<int>.FromRedisValue(RedisValue value, string valueType) => (int)value;

	RedisValue IRedisValueConverter<uint>.ToRedisValue(uint value) => value;

	uint IRedisValueConverter<uint>.FromRedisValue(RedisValue value, string valueType) => (uint)value;

	RedisValue IRedisValueConverter<short>.ToRedisValue(short value) => value;

	short IRedisValueConverter<short>.FromRedisValue(RedisValue value, string valueType) => (short)value;

	RedisValue IRedisValueConverter<float>.ToRedisValue(float value) => (double)value;

	float IRedisValueConverter<float>.FromRedisValue(RedisValue value, string valueType) => (float)(double)value;

	RedisValue IRedisValueConverter<double>.ToRedisValue(double value) => value;

	double IRedisValueConverter<double>.FromRedisValue(RedisValue value, string valueType) => (double)value;

	RedisValue IRedisValueConverter<bool>.ToRedisValue(bool value) => value;

	bool IRedisValueConverter<bool>.FromRedisValue(RedisValue value, string valueType) => (bool)value;

	RedisValue IRedisValueConverter<long>.ToRedisValue(long value) => value;

	long IRedisValueConverter<long>.FromRedisValue(RedisValue value, string valueType) => (long)value;

	// ulong 可能超出 Redis 可存储整数（有符号 64 位整数）的上限；
	// 即使不超过 long.MaxValue，StackExchange.Redis 客户端也会无故按 double 存储，因此强制转换为 long。
	RedisValue IRedisValueConverter<ulong>.ToRedisValue(ulong value) => value > long.MaxValue ? (RedisValue)value.ToString() : checked((long)value);

	ulong IRedisValueConverter<ulong>.FromRedisValue(RedisValue value, string valueType) => ulong.Parse(value.ToString());

	RedisValue IRedisValueConverter<object>.ToRedisValue(object value)
	{
		var valueType = value.GetType();
		if (valueType == _byteArrayType)
		{
			var converter = (IRedisValueConverter<byte[]>)this;
			return converter.ToRedisValue((byte[])value);
		}

		if (valueType == _stringType)
		{
			var converter = (IRedisValueConverter<string>)this;
			return converter.ToRedisValue((string)value);
		}

		if (valueType == _intType)
		{
			var converter = (IRedisValueConverter<int>)this;
			return converter.ToRedisValue((int)value);
		}

		if (valueType == _uIntType)
		{
			var converter = (IRedisValueConverter<uint>)this;
			return converter.ToRedisValue((uint)value);
		}

		if (valueType == _shortType)
		{
			var converter = (IRedisValueConverter<short>)this;
			return converter.ToRedisValue((short)value);
		}

		if (valueType == _singleType)
		{
			var converter = (IRedisValueConverter<float>)this;
			return converter.ToRedisValue((float)value);
		}

		if (valueType == _doubleType)
		{
			var converter = (IRedisValueConverter<double>)this;
			return converter.ToRedisValue((double)value);
		}

		if (valueType == _boolType)
		{
			var converter = (IRedisValueConverter<bool>)this;
			return converter.ToRedisValue((bool)value);
		}

		if (valueType == _longType)
		{
			var converter = (IRedisValueConverter<long>)this;
			return converter.ToRedisValue((long)value);
		}

		if (valueType == _uLongType)
		{
			var converter = (IRedisValueConverter<ulong>)this;
			return converter.ToRedisValue((ulong)value);
		}

		{
		}
		return JsonSerializer.Serialize(value);
	}

	object IRedisValueConverter<object>.FromRedisValue(RedisValue value, string type)
	{
		var valueType = TypeCache.GetType(type);

		if (valueType == _byteArrayType)
		{
			var converter = (IRedisValueConverter<byte[]>)this;
			return converter.FromRedisValue(value, type);
		}

		if (valueType == _stringType)
		{
			var converter = (IRedisValueConverter<string>)this;
			return converter.FromRedisValue(value, type);
		}

		if (valueType == _intType)
		{
			var converter = (IRedisValueConverter<int>)this;
			return converter.FromRedisValue(value, type);
		}

		if (valueType == _uIntType)
		{
			var converter = (IRedisValueConverter<uint>)this;
			return converter.FromRedisValue(value, type);
		}

		if (valueType == _shortType)
		{
			var converter = (IRedisValueConverter<short>)this;
			return converter.FromRedisValue(value, type);
		}

		if (valueType == _singleType)
		{
			var converter = (IRedisValueConverter<float>)this;
			return converter.FromRedisValue(value, type);
		}

		if (valueType == _doubleType)
		{
			var converter = (IRedisValueConverter<double>)this;
			return converter.FromRedisValue(value, type);
		}

		if (valueType == _boolType)
		{
			var converter = (IRedisValueConverter<bool>)this;
			return converter.FromRedisValue(value, type);
		}

		if (valueType == _longType)
		{
			var converter = (IRedisValueConverter<long>)this;
			return converter.FromRedisValue(value, type);
		}

		if (valueType == _uLongType)
		{
			var converter = (IRedisValueConverter<ulong>)this;
			return converter.FromRedisValue(value, type);
		}

		{
		}
		return Deserialize(value.ToString(), type);
	}

	/// <summary>
	/// 使用 JSON 把指定值序列化为 Redis 值。
	/// </summary>
	/// <typeparam name="T">值的类型。</typeparam>
	/// <param name="value">要序列化的值。</param>
	/// <returns>JSON 文本形式的 Redis 值。</returns>
	public RedisValue ToRedisValue<T>(T value) => JsonSerializer.Serialize(value);

	/// <summary>
	/// 按类型名把 Redis 中的 JSON 文本反序列化为 <typeparamref name="T"/>。
	/// </summary>
	/// <typeparam name="T">期望的返回类型。</typeparam>
	/// <param name="value">Redis 中存储的值。</param>
	/// <param name="valueType">原始值类型的名称。</param>
	/// <returns>反序列化后的值。</returns>
	public T FromRedisValue<T>(RedisValue value, string valueType) => (T)Deserialize(value.ToString(), valueType);

	private static object Deserialize(string value, string valueType)
	{
		var type = TypeCache.GetType(valueType);
		if (type == null)
		{
			throw new NullReferenceException(string.Format(Resources.IDS_TYPE_COULD_NOT_BE_LOADED, valueType));
		}

		return JsonSerializer.Deserialize(value, type)!;
	}
}