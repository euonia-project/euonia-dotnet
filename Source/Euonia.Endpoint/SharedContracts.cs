using System.Runtime.Serialization;

namespace Nerosoft.Euonia.Endpoint;

/// <summary>
/// 空消息，用于无返回值或空请求的 gRPC 方法。
/// </summary>
[DataContract]
public sealed class EndpointEmpty
{
	/// <summary>
	/// 获取共享实例。
	/// </summary>
	public static EndpointEmpty Instance { get; } = new();
}

/// <summary>
/// <see cref="decimal"/> 的 proto 契约表示。
/// </summary>
/// <remarks>支持 9 位小数；超出 <see cref="long"/> 范围的整数部分会抛出 <see cref="ArgumentOutOfRangeException"/>。当前生成的 gRPC 契约尚未使用此类型，保留给后续 wire-DTO 映射。</remarks>
[DataContract]
public sealed class DecimalValue
{
	/// <summary>
	/// 整数部分。
	/// </summary>
	[DataMember(Order = 1)]
	public long Units { get; set; }

	/// <summary>
	/// 小数部分，单位为 10^-9。
	/// </summary>
	[DataMember(Order = 2)]
	public int Nanos { get; set; }

	/// <summary>
	/// 把 <see cref="decimal"/> 转换为 <see cref="DecimalValue"/>。
	/// </summary>
	public static DecimalValue FromDecimal(decimal value)
	{
		if (value > long.MaxValue || value < long.MinValue)
		{
			throw new ArgumentOutOfRangeException(nameof(value), "The value is outside the supported DecimalValue range.");
		}

		var units = decimal.Truncate(value);
		var nanos = (int)decimal.Round((value - units) * 1_000_000_000m, 0, MidpointRounding.AwayFromZero);
		if (nanos >= 1_000_000_000)
		{
			units += 1;
			nanos -= 1_000_000_000;
		}
		else if (nanos <= -1_000_000_000)
		{
			units -= 1;
			nanos += 1_000_000_000;
		}

		return new DecimalValue { Units = (long)units, Nanos = nanos };
	}

	/// <summary>
	/// 把 <see cref="DecimalValue"/> 转换为 <see cref="decimal"/>。
	/// </summary>
	public decimal ToDecimal() => Units + Nanos / 1_000_000_000m;

	/// <summary>
	/// 隐式转换为 <see cref="DecimalValue"/>。
	/// </summary>
	public static implicit operator DecimalValue(decimal value) => FromDecimal(value);

	/// <summary>
	/// 隐式转换为 <see cref="decimal"/>。
	/// </summary>
	public static implicit operator decimal(DecimalValue value) => value.ToDecimal();
}

/// <summary>
/// <see cref="Guid"/> 的 proto 契约表示，使用标准 D 格式字符串。
/// </summary>
/// <remarks>当前生成的 gRPC 契约尚未使用此类型，保留给后续 wire-DTO 映射。</remarks>
[DataContract]
public sealed class GuidValue
{
	/// <summary>
	/// 标准 D 格式的 GUID 字符串。
	/// </summary>
	[DataMember(Order = 1)]
	public string Value { get; set; } = string.Empty;

	/// <summary>
	/// 隐式转换为 <see cref="GuidValue"/>。
	/// </summary>
	public static implicit operator GuidValue(Guid value) => new() { Value = value.ToString("D") };

	/// <summary>
	/// 隐式转换为 <see cref="Guid"/>。
	/// </summary>
	public static implicit operator Guid(GuidValue value) => Guid.Parse(value.Value);
}

/// <summary>
/// <see cref="DateTime"/> 的 proto 契约表示，等价于 google.protobuf.Timestamp。
/// </summary>
/// <remarks>当前生成的 gRPC 契约尚未使用此类型，保留给后续 wire-DTO 映射。</remarks>
[DataContract]
public sealed class TimestampValue
{
	private static readonly DateTime _unixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

	/// <summary>
	/// Unix 秒。
	/// </summary>
	[DataMember(Order = 1)]
	public long Seconds { get; set; }

	/// <summary>
	/// 纳秒部分。
	/// </summary>
	[DataMember(Order = 2)]
	public int Nanos { get; set; }

	/// <summary>
	/// 隐式转换为 <see cref="TimestampValue"/>。
	/// </summary>
	public static implicit operator TimestampValue(DateTime value)
	{
		var utc = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();
		var ticks = utc.Ticks - _unixEpoch.Ticks;
		var seconds = ticks / TimeSpan.TicksPerSecond;
		var remainder = ticks % TimeSpan.TicksPerSecond;
		if (remainder < 0)
		{
			seconds--;
			remainder += TimeSpan.TicksPerSecond;
		}

		return new TimestampValue
		{
			Seconds = seconds,
			Nanos = (int)(remainder * 100)
		};
	}

	/// <summary>
	/// 隐式转换为 <see cref="DateTime"/>。
	/// </summary>
	public static implicit operator DateTime(TimestampValue value)
	{
		return _unixEpoch.AddSeconds(value.Seconds).AddTicks(value.Nanos / 100);
	}
}

/// <summary>
/// <see cref="TimeSpan"/> 的 proto 契约表示，等价于 google.protobuf.Duration。
/// </summary>
/// <remarks>当前生成的 gRPC 契约尚未使用此类型，保留给后续 wire-DTO 映射。</remarks>
[DataContract]
public sealed class DurationValue
{
	/// <summary>
	/// 秒。
	/// </summary>
	[DataMember(Order = 1)]
	public long Seconds { get; set; }

	/// <summary>
	/// 纳秒。
	/// </summary>
	[DataMember(Order = 2)]
	public int Nanos { get; set; }

	/// <summary>
	/// 隐式转换为 <see cref="DurationValue"/>。
	/// </summary>
	public static implicit operator DurationValue(TimeSpan value)
	{
		var seconds = value.Ticks / TimeSpan.TicksPerSecond;
		var remainder = value.Ticks % TimeSpan.TicksPerSecond;
		if (remainder < 0)
		{
			seconds--;
			remainder += TimeSpan.TicksPerSecond;
		}

		return new DurationValue
		{
			Seconds = seconds,
			Nanos = (int)(remainder * 100)
		};
	}

	/// <summary>
	/// 隐式转换为 <see cref="TimeSpan"/>。
	/// </summary>
	public static implicit operator TimeSpan(DurationValue value)
	{
		return TimeSpan.FromSeconds(value.Seconds).Add(TimeSpan.FromTicks(value.Nanos / 100));
	}
}

/// <summary>
/// <see cref="DateOnly"/> 的 proto 契约表示。
/// </summary>
/// <remarks>当前生成的 gRPC 契约尚未使用此类型，保留给后续 wire-DTO 映射。</remarks>
[DataContract]
public sealed class DateOnlyValue
{
	/// <summary>
	/// 年。
	/// </summary>
	[DataMember(Order = 1)]
	public int Year { get; set; }

	/// <summary>
	/// 月。
	/// </summary>
	[DataMember(Order = 2)]
	public int Month { get; set; }

	/// <summary>
	/// 日。
	/// </summary>
	[DataMember(Order = 3)]
	public int Day { get; set; }

	/// <summary>
	/// 隐式转换为 <see cref="DateOnlyValue"/>。
	/// </summary>
	public static implicit operator DateOnlyValue(DateOnly value) => new() { Year = value.Year, Month = value.Month, Day = value.Day };

	/// <summary>
	/// 隐式转换为 <see cref="DateOnly"/>。
	/// </summary>
	public static implicit operator DateOnly(DateOnlyValue value) => new(value.Year, value.Month, value.Day);
}

/// <summary>
/// <see cref="TimeOnly"/> 的 proto 契约表示。
/// </summary>
/// <remarks>当前生成的 gRPC 契约尚未使用此类型，保留给后续 wire-DTO 映射。</remarks>
[DataContract]
public sealed class TimeOnlyValue
{
	/// <summary>
	/// 时。
	/// </summary>
	[DataMember(Order = 1)]
	public int Hour { get; set; }

	/// <summary>
	/// 分。
	/// </summary>
	[DataMember(Order = 2)]
	public int Minute { get; set; }

	/// <summary>
	/// 秒。
	/// </summary>
	[DataMember(Order = 3)]
	public int Second { get; set; }

	/// <summary>
	/// 纳秒。
	/// </summary>
	[DataMember(Order = 4)]
	public int Nanosecond { get; set; }

	/// <summary>
	/// 隐式转换为 <see cref="TimeOnlyValue"/>。
	/// </summary>
	public static implicit operator TimeOnlyValue(TimeOnly value)
	{
		return new TimeOnlyValue
		{
			Hour = value.Hour,
			Minute = value.Minute,
			Second = value.Second,
			Nanosecond = (int)((value.Ticks % TimeSpan.TicksPerSecond) * 100)
		};
	}

	/// <summary>
	/// 隐式转换为 <see cref="TimeOnly"/>。
	/// </summary>
	public static implicit operator TimeOnly(TimeOnlyValue value)
	{
		return new TimeOnly(value.Hour, value.Minute, value.Second).Add(TimeSpan.FromTicks(value.Nanosecond / 100));
	}
}
