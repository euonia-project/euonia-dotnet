using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 配置一个 <see cref="IScheduleBuilder" />，用于为触发器定义基于日历时间（日、周、月、年）间隔的调度。
/// </summary>
/// <seealso cref="CalendarIntervalScheduleBuilder"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class CalendarIntervalScheduleAttribute : BackgroundJobScheduleAttribute
{
	/// <summary>
	/// 初始化 <see cref="CalendarIntervalScheduleAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="identity">触发器的标识，配置触发器时会附加 <c>.trigger</c> 后缀作为触发器名称。</param>
	/// <param name="interval">触发器重复的间隔数值，与 <paramref name="unit"/> 共同决定实际时间间隔。</param>
	/// <param name="unit">间隔的日历时间单位（如秒、分钟、小时、天、周、月、年）。</param>
	public CalendarIntervalScheduleAttribute(string identity, int interval, IntervalUnit unit)
		: base(identity)
	{
		Interval = interval;
		Unit = unit;
	}

	/// <summary>
	/// 获取触发器重复的间隔。
	/// </summary>
	public int Interval { get; }

	/// <summary>
	/// 获取间隔的时间单位。
	/// </summary>
	public IntervalUnit Unit { get; }

	/// <summary>
	/// 获取或设置一个值，指示当触发小时因夏令时切换而不存在时是否跳过当天。
	/// </summary>
	/// <remarks>
	/// 为 <see langword="null"/> 时不覆盖 <see cref="CalendarIntervalScheduleBuilder"/> 的默认行为。
	/// </remarks>
	public bool? SkipDayIfHourDoesNotExist { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示跨夏令时切换时是否保持当天的触发小时不变。
	/// </summary>
	/// <remarks>
	/// 为 <see langword="null"/> 时不覆盖 <see cref="CalendarIntervalScheduleBuilder"/> 的默认行为；
	/// 启用后实际触发间隔可能不再是标准的日历间隔。
	/// </remarks>
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
