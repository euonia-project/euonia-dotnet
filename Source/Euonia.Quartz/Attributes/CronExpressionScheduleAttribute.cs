using System.Globalization;
using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 表示 cron 表达式触发器特性。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CronExpressionScheduleAttribute : BackgroundJobScheduleAttribute
{
	/// <summary>
	/// 初始化 <see cref="CronExpressionScheduleAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="identity">触发器标识。</param>
	/// <param name="cronExpression">用于调度的 cron 表达式，不能为 <see langword="null"/> 或空白。</param>
	/// <exception cref="ArgumentNullException">当 <paramref name="cronExpression"/> 为 <see langword="null"/>、空字符串或仅含空白字符时抛出。</exception>
	public CronExpressionScheduleAttribute(string identity, string cronExpression)
		: base(identity)
	{
		if (string.IsNullOrWhiteSpace(cronExpression))
		{
			throw new ArgumentNullException(nameof(cronExpression));
		}

		CronExpression = cronExpression;
	}

	/// <summary>
	/// 初始化 <see cref="CronExpressionScheduleAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="identity">触发器标识。</param>
	/// <param name="hour">触发的小时（0–23）。</param>
	/// <param name="minute">触发的分钟（0–59）。</param>
	/// <param name="daysOfWeek">指定每周触发的星期几；为空时不限制星期，即每天触发。</param>
	/// <exception cref="ArgumentOutOfRangeException">当 <paramref name="hour"/> 不在 0–23 或 <paramref name="minute"/> 不在 0–59 范围内时抛出。</exception>
	/// <remarks>生成按周重复的 cron 表达式；未提供 <paramref name="daysOfWeek"/> 时每天在指定时间触发。</remarks>
	public CronExpressionScheduleAttribute(string identity, int hour, int minute, params DayOfWeek[] daysOfWeek)
		: base(identity)
	{
		ValidateHour(hour);
		ValidateMinute(minute);

		if (daysOfWeek?.Length > 0)
		{
			var abbreviations = daysOfWeek.Select(GetAbbreviation).JoinAsString(",");
			CronExpression = $"0 {minute} {hour} ? * {abbreviations}";
		}
		else
		{
			CronExpression = $"0 {minute} {hour} ? * *";
		}
	}

	/// <summary>
	/// 初始化 <see cref="CronExpressionScheduleAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="identity">触发器标识。</param>
	/// <param name="hour">触发的小时（0–23）。</param>
	/// <param name="minute">触发的分钟（0–59）。</param>
	/// <param name="dayOfMonth">每月触发的日（1–31）。</param>
	/// <exception cref="ArgumentOutOfRangeException">当 <paramref name="dayOfMonth"/> 不在 1–31、<paramref name="hour"/> 不在 0–23 或 <paramref name="minute"/> 不在 0–59 范围内时抛出。</exception>
	/// <remarks>生成按月在指定日与时间触发的 cron 表达式。</remarks>
	public CronExpressionScheduleAttribute(string identity, int hour, int minute, int dayOfMonth)
		: base(identity)
	{
		ValidateDayOfMonth(dayOfMonth);
		ValidateHour(hour);
		ValidateMinute(minute);

		CronExpression = $"0 {minute} {hour} {dayOfMonth} * ?";
	}

	/// <summary>
	/// 获取 crontab 表达式。
	/// </summary>
	public string CronExpression { get; }

	/// <summary>
	/// 获取或设置作业时区名称。
	/// 请参阅 https://docs.microsoft.com/en-us/dotnet/standard/base-types/time-zone-names-and-ids
	/// 也可以使用 <see cref="TimeZoneInfo.GetSystemTimeZones()"/> 方法获取时区名称。
	/// </summary>
	public string TimeZoneName { get; set; } = "UTC";

	/// <inheritdoc/>
	/// <exception cref="FormatException">当 <see cref="CronExpression"/> 不是有效的 cron 表达式时抛出。</exception>
	/// <exception cref="TimeZoneNotFoundException">当 <see cref="TimeZoneName"/> 不是系统已知的时区标识时抛出。</exception>
	/// <exception cref="InvalidTimeZoneException">当 <see cref="TimeZoneName"/> 对应的时区数据无效时抛出。</exception>
	public override IScheduleBuilder Configure()
	{
		var builder = CronScheduleBuilder.Create(CronExpression);

		var timeZoneInfo = TimeZoneName.ToUpper(CultureInfo.CurrentCulture) switch
		{
			"UTC" => TimeZoneInfo.Utc,
			"LOCAL" => TimeZoneInfo.Local,
			_ => TimeZoneInfo.FindSystemTimeZoneById(TimeZoneName)
		};
		builder.InTimeZone(timeZoneInfo);

		if (MisfirePolicy.HasValue)
		{
			builder.WithMisfireInstruction((CronTriggerMisfireInstruction)MisfirePolicy.Value);
		}

		return builder;
	}

	/// <summary>
	/// 获取 <see cref="DayOfWeek"/> 的缩写。
	/// </summary>
	/// <param name="dayOfWeek">要转换的星期几。</param>
	/// <returns>指定星期几的缩写名称（例如 SUN、MON、TUE、WED、THU、FRI、SAT）；未定义的枚举值返回其数值字符串。</returns>
	private static string GetAbbreviation(DayOfWeek dayOfWeek)
	{
		return dayOfWeek switch
		{
			DayOfWeek.Sunday => "SUN",
			DayOfWeek.Monday => "MON",
			DayOfWeek.Tuesday => "TUE",
			DayOfWeek.Wednesday => "WED",
			DayOfWeek.Thursday => "THU",
			DayOfWeek.Friday => "FRI",
			DayOfWeek.Saturday => "SAT",
			_ => ((int)dayOfWeek).ToString()
		};
	}

	private static void ValidateHour(int hour)
	{
		if (hour is < 0 or > 23)
		{
			throw new ArgumentOutOfRangeException(nameof(hour), hour, "Hour of day must be between 0 and 23, inclusive.");
		}
	}

	private static void ValidateMinute(int minute)
	{
		if (minute is < 0 or > 59)
		{
			throw new ArgumentOutOfRangeException(nameof(minute), minute, "Minute of hour must be between 0 and 59, inclusive.");
		}
	}

	private static void ValidateDayOfMonth(int dayOfMonth)
	{
		if (dayOfMonth is < 1 or > 31)
		{
			throw new ArgumentOutOfRangeException(nameof(dayOfMonth), dayOfMonth, "Day of month must be between 1 and 31, inclusive.");
		}
	}
}
