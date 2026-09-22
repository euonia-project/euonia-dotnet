using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Nerosoft.Euonia.Repository.EfCore;

/// <summary>
/// 定义 <see cref="DateTime"/> 值与 UTC 时间之间的转换规则。
/// </summary>
/// <remarks>
/// 写入数据库时将本地时间或未指定类型的时间统一转换为 UTC；
/// 从数据库读取时按 UTC 解释并转换为本地时间，以避免因时区差异导致的日期时间偏差。
/// </remarks>
public class UniversalTimeConverter : ValueConverter<DateTime, DateTime>
{
	/// <summary>
	/// 初始化 <see cref="UniversalTimeConverter"/> 类的新实例。
	/// </summary>
	public UniversalTimeConverter()
		: base(t => ConvertToUniversalTime(t), t => ConvertToLocalTime(t))
	{
	}

	private static DateTime ConvertToUniversalTime(DateTime time)
	{
		return time.Kind switch
		{
			DateTimeKind.Unspecified => DateTime.SpecifyKind(time, DateTimeKind.Local).ToUniversalTime(),
			DateTimeKind.Local => time.ToUniversalTime(),
			DateTimeKind.Utc => time,
			_ => time
		};
	}

	private static DateTime ConvertToLocalTime(DateTime time)
	{
		return time.Kind switch
		{
			DateTimeKind.Unspecified => DateTime.SpecifyKind(time, DateTimeKind.Utc).ToLocalTime(),
			DateTimeKind.Utc => time.ToLocalTime(),
			DateTimeKind.Local => time,
			_ => time
		};
	}
}
