using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DailyTimeIntervalScheduleAttribute : BackgroundJobScheduleAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="DailyTimeIntervalScheduleAttribute"/> class.
	/// </summary>
	/// <param name="identity"></param>
	/// <param name="interval"></param>
	/// <param name="unit"></param>
	public DailyTimeIntervalScheduleAttribute(string identity, int interval, IntervalUnit unit)
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
	/// Gets or set number of times for interval to repeat.
	/// </summary>
	public int? RepeatCount { get; set; }

	/// <inheritdoc />
	public override IScheduleBuilder Configure()
	{
		var builder = DailyTimeIntervalScheduleBuilder.Create();
		builder.WithInterval(Interval, Unit);

		if (RepeatCount > 0)
		{
			builder.WithRepeatCount(RepeatCount.Value);
		}

		if (MisfirePolicy.HasValue)
		{
			builder.WithMisfireInstruction((DailyTimeIntervalTriggerMisfireInstruction)MisfirePolicy.Value);
		}

		return builder;
	}
}
