using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// Configure a <see cref="IScheduleBuilder" /> that defines calendar time (day, week, month, year) interval-based schedules for Triggers.
/// </summary>
/// <seealso cref="CalendarIntervalScheduleBuilder"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CalendarIntervalScheduleAttribute : BackgroundJobScheduleAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="CalendarIntervalScheduleAttribute"/> class.
	/// </summary>
	/// <param name="identity"></param>
	/// <param name="interval"></param>
	/// <param name="unit"></param>
	public CalendarIntervalScheduleAttribute(string identity, int interval, IntervalUnit unit)
		: base(identity)
	{
		Interval = interval;
		Unit = unit;
	}

	/// <summary>
	/// Gets the interval at which the trigger should repeat.
	/// </summary>
	public int Interval { get; }

	/// <summary>
	/// Gets the time unit of the interval.
	/// </summary>
	public IntervalUnit Unit { get; }

	/// <summary>
	/// 
	/// </summary>
	public bool? SkipDayIfHourDoesNotExist { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public bool? PreserveHourOfDayAcrossDaylightSavings { get; set; }

	/// <inheritdoc />
	public override IScheduleBuilder Configure()
	{
		var builder = CalendarIntervalScheduleBuilder.Create();
		builder.WithInterval(Interval, Unit);

		if (MisfirePolicy.HasValue)
		{
			builder.WithMisfireInstruction((CalendarIntervalTriggerMisfireInstruction)MisfirePolicy.Value);
		}

		if (SkipDayIfHourDoesNotExist.HasValue)
		{
			builder.SkipDayIfHourDoesNotExist(SkipDayIfHourDoesNotExist.Value);
		}
		if (PreserveHourOfDayAcrossDaylightSavings.HasValue)
		{
			builder.PreserveHourOfDayAcrossDaylightSavings(PreserveHourOfDayAcrossDaylightSavings.Value);
		}
		return builder;
	}
}
