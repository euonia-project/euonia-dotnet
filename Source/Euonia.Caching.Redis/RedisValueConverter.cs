using System.Text.Json;
using Nerosoft.Euonia.Caching.Internal;
using StackExchange.Redis;

namespace Nerosoft.Euonia.Caching.Redis;

internal interface IRedisValueConverter
{
	RedisValue ToRedisValue<T>(T value);

	T FromRedisValue<T>(RedisValue value, string valueType);
}

internal interface IRedisValueConverter<T>
{
	RedisValue ToRedisValue(T value);

	T FromRedisValue(RedisValue value, string valueType);
}

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

	byte[] IRedisValueConverter<byte[]>.FromRedisValue(RedisValue value, string valueType) => value;

	RedisValue IRedisValueConverter<string>.ToRedisValue(string value) => value;

	string IRedisValueConverter<string>.FromRedisValue(RedisValue value, string valueType) => value;

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

	// ulong can exceed the supported lenght of storing integers (which is signed 64bit integer)
	// also, even if we do not exceed long.MaxValue, the SA client stores it as double for no parent reason => cast to long fixes it.
	RedisValue IRedisValueConverter<ulong>.ToRedisValue(ulong value) => value > long.MaxValue ? (RedisValue)value.ToString() : checked((long)value);

	ulong IRedisValueConverter<ulong>.FromRedisValue(RedisValue value, string valueType) => ulong.Parse(value);

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
		return Deserialize(value, type);
	}

	public RedisValue ToRedisValue<T>(T value) => JsonSerializer.Serialize(value);

	public T FromRedisValue<T>(RedisValue value, string valueType) => (T)Deserialize(value, valueType);

	private static object Deserialize(string value, string valueType)
	{
		var type = TypeCache.GetType(valueType);
		if (type == null)
		{
			throw new NullReferenceException(string.Format(Resources.IDS_TYPE_COULD_NOT_BE_LOADED, valueType));
		}

		return JsonSerializer.Deserialize(value, type);
	}
}