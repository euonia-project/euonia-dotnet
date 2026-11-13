using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 配置一个 <see cref="IScheduleBuilder" />，用于为 <see cref="ITrigger" /> 定义严格/字面意义上的固定间隔调度。
/// </summary>
/// <seealso cref="SimpleScheduleBuilder"/>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class SimpleScheduleAttribute : BackgroundJobScheduleAttribute
{
	/// <summary>
	/// 初始化 <see cref="SimpleScheduleAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="identity">触发器的标识，配置触发器时会附加 <c>.trigger</c> 后缀作为触发器名称。</param>
	/// <param name="milliseconds">以毫秒表示的固定触发间隔。</param>
	public SimpleScheduleAttribute(string identity, int milliseconds)
		: base(identity)
	{
		Interval = TimeSpan.FromMilliseconds(milliseconds);
	}

	/// <summary>
	/// 初始化 <see cref="SimpleScheduleAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="identity">触发器的标识，配置触发器时会附加 <c>.trigger</c> 后缀作为触发器名称。</param>
	/// <param name="hour">间隔的小时部分（取值范围 -23 到 23）。</param>
	/// <param name="minute">间隔的分钟部分（取值范围 -59 到 59）。</param>
	/// <param name="second">间隔的秒部分（取值范围 -59 到 59）。</param>
	/// <exception cref="ArgumentOutOfRangeException">当 <paramref name="hour"/>、<paramref name="minute"/> 或 <paramref name="second"/> 超出 <see cref="TimeSpan"/> 构造函数允许的范围时抛出。</exception>
	public SimpleScheduleAttribute(string identity, int hour, int minute, int second)
		: base(identity)
	{
		Interval = new TimeSpan(hour, minute, second);
	}

	/// <summary>
	/// 获取触发器间隔。
	/// </summary>
	public TimeSpan Interval { get; }

	/// <summary>
	/// 获取或设置触发器重复的次数。
	/// 总触发次数为该数值加 1。
	/// 若要永久重复，请保留为 null。
	/// </summary>
	public int? RepeatCount { get; set; }

	/// <inheritdoc/>
	/// <exception cref="InvalidOperationException">当 <see cref="RepeatCount"/> 小于 0 时抛出。</exception>
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
