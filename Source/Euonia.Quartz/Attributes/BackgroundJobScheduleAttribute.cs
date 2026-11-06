using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// The abstract base class for background job schedule attributes.
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class BackgroundJobScheduleAttribute : Attribute
{
	/// <summary>
	/// Initializes a new instance of the <see cref="BackgroundJobScheduleAttribute"/> class.
	/// </summary>
	/// <param name="identity"></param>
	protected BackgroundJobScheduleAttribute(string identity)
	{
		Identity = identity;
	}

	/// <summary>
	/// Gets the trigger identity.
	/// </summary>
	public string Identity { get; }

	/// <summary>
	/// Gets or sets the start delay time in seconds.
	/// 0 means start immediately.
	/// </summary>
	public int Delay { get; set; }

	/// <summary>
	/// Gets or sets the trigger priority.
	/// </summary>
	public int Priority { get; set; }

	/// <summary>
	/// Gets or sets the trigger description.
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// Gets or sets the numeric misfire instruction applied to the trigger.
	/// <br/>
	/// <para>The value is interpreted as <see cref="CronTriggerMisfireInstruction"/> for <see cref="CronExpressionScheduleAttribute"/>,
	/// <see cref="SimpleTriggerMisfireInstruction"/> for <see cref="SimpleScheduleAttribute"/>,
	/// <see cref="CalendarIntervalTriggerMisfireInstruction"/> for <see cref="CalendarIntervalScheduleAttribute"/> and
	/// <see cref="DailyTimeIntervalTriggerMisfireInstruction"/> for <see cref="DailyTimeIntervalScheduleAttribute"/>.</para>
	/// <para><c>-1</c> (<c>IgnoreMisfires</c>) is valid for every schedule type.</para>
	/// </summary>
	/// <remarks>
	/// Default value: <see langword="null"/>, which leaves the schedule's smart policy in place.
	/// </remarks>
	public virtual int? MisfirePolicy { get; set; }

	/// <summary>
	/// Configures the trigger builder.
	/// </summary>
	/// <returns></returns>
	public abstract IScheduleBuilder Configure();
}
