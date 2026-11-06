using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 配置基于 <see cref="DailyTimeIntervalScheduleBuilder"/> 的每日时间区间调度，使触发器按固定间隔重复执行。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class DailyTimeIntervalScheduleAttribute : BackgroundJobScheduleAttribute
{
	/// <summary>
	/// 初始化 <see cref="DailyTimeIntervalScheduleAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="identity">触发器的标识，配置触发器时会附加 <c>.trigger</c> 后缀作为触发器名称。</param>
	/// <param name="interval">触发器重复的间隔数值，与 <paramref name="unit"/> 共同决定实际时间间隔。</param>
	/// <param name="unit">间隔的时间单位（如秒、分钟、小时、天、周、月、年）。</param>
	public DailyTimeIntervalScheduleAttribute(string identity, int interval, IntervalUnit unit)
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
	/// 获取或设置间隔重复的次数。
	/// </summary>
	/// <remarks>
	/// 仅当值大于 0 时才会应用到调度构建器；为 <see langword="null"/> 或非正数时保持 Quartz 的默认行为。
	/// </remarks>
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
