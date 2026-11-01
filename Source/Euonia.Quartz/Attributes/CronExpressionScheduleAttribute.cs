using System.Globalization;
using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// Represents a cron expression trigger attribute.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CronExpressionScheduleAttribute : BackgroundJobScheduleAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CronExpressionScheduleAttribute"/> class.
	/// </summary>
	/// <param name="identity">The trigger identity.</param>
	/// <param name="cronExpression">The cron expression for schedule.</param>
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
	/// Initializes a new instance of the <see cref="CronExpressionScheduleAttribute"/> class.
	/// </summary>
	/// <param name="identity"></param>
	/// <param name="hour"></param>
	/// <param name="minute"></param>
	/// <param name="daysOfWeek"></param>
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
	/// Initializes a new instance of the &lt;see cref="CronExpressionScheduleAttribute"/&gt; class.
	/// </summary>
	/// <param name="identity"></param>
	/// <param name="hour"></param>
	/// <param name="minute"></param>
	/// <param name="dayOfMonth"></param>
	public CronExpressionScheduleAttribute(string identity, int hour, int minute, int dayOfMonth)
		: base(identity)
	{
		ValidateDayOfMonth(dayOfMonth);
		ValidateHour(hour);
		ValidateMinute(minute);

		CronExpression = $"0 {minute} {hour} {dayOfMonth} * ?";
	}

	/// <summary>
	/// Gets the crontab expression.
	/// </summary>
	public string CronExpression { get; }

	/// <summary>
	/// Gets or sets the job time zone name.
	/// Please check https://docs.microsoft.com/en-us/dotnet/standard/base-types/time-zone-names-and-ids
	/// Or you can get the time zone names using method <see cref="TimeZoneInfo.GetSystemTimeZones()"/>
	/// </summary>
	public string TimeZoneName { get; set; } = "UTC";

	/// <inheritdoc/>
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
	/// Gets the abbreviation of <see cref="DayOfWeek"/>.
	/// </summary>
	/// <param name="dayOfWeek"></param>
	/// <returns>The abbreviated day name (e.g. SUN,MON,TUE,WED,THU,FRI,SAT) of specified day of week.</returns>
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
