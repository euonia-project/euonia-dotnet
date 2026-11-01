using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// Configure a <see cref="IScheduleBuilder" /> that defines strict/literal interval-based schedules for <see cref="ITrigger" />s.
/// </summary>
/// <seealso cref="SimpleScheduleBuilder"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SimpleScheduleAttribute : BackgroundJobScheduleAttribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="SimpleScheduleAttribute"/> class.
	/// </summary>
	/// <param name="identity"></param>
	/// <param name="milliseconds"></param>
	public SimpleScheduleAttribute(string identity, int milliseconds)
		: base(identity)
	{
		Interval = TimeSpan.FromMilliseconds(milliseconds);
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="SimpleScheduleAttribute"/> class.
	/// </summary>
	/// <param name="identity"></param>
	/// <param name="hour"></param>
	/// <param name="minute"></param>
	/// <param name="second"></param>
	public SimpleScheduleAttribute(string identity, int hour, int minute, int second)
		: base(identity)
	{
		Interval = new TimeSpan(hour, minute, second);
	}

	/// <summary>
	/// Gets the trigger interval.
	/// </summary>
	public TimeSpan Interval { get; }

	/// <summary>
	/// Gets or sets the number of time that the trigger will repeat.
	/// Total number of firings will be this number + 1.
	/// Leave it null if you want to repeat forever.
	/// </summary>
	public int? RepeatCount { get; set; }

	/// <inheritdoc/>
	public override IScheduleBuilder Configure()
	{
		var builder = SimpleScheduleBuilder.Create();

		builder.WithInterval(Interval);
		if (RepeatCount.HasValue)
		{
			if (RepeatCount.Value < 0)
			{
				throw new InvalidOperationException(Resources.IDS_REPEAT_COUNT_MUST_BE_NON_NEGATIVE);
			}

			builder.WithRepeatCount(RepeatCount.Value);
		}
		else
		{
			builder.RepeatForever();
		}

		if (MisfirePolicy.HasValue)
		{
			builder.WithMisfireInstruction((SimpleTriggerMisfireInstruction)MisfirePolicy.Value);
		}

		return builder;
	}
}
