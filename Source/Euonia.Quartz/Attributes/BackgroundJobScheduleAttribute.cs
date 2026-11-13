using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 后台作业调度特性的抽象基类。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public abstract class BackgroundJobScheduleAttribute : Attribute
{
	/// <summary>
	/// 初始化 <see cref="BackgroundJobScheduleAttribute"/> 类的新实例。
	/// </summary>
	/// <param name="identity">触发器的标识，配置触发器时会附加 <c>.trigger</c> 后缀作为触发器名称。</param>
	protected BackgroundJobScheduleAttribute(string identity)
	{
		Identity = identity;
	}

	/// <summary>
	/// 获取触发器标识。
	/// </summary>
	public string Identity { get; }

	/// <summary>
	/// 获取或设置启动延迟时间（以秒为单位）。
	/// 小于或等于 0 表示立即启动。
	/// </summary>
	public int Delay { get; set; }

	/// <summary>
	/// 获取或设置触发器优先级。
	/// </summary>
	public int Priority { get; set; }

	/// <summary>
	/// 获取或设置触发器说明。
	/// </summary>
	public string? Description { get; set; }

	/// <summary>
	/// 获取或设置应用于触发器的数字形式的失火指令。
	/// <br/>
	/// <para>对于 <see cref="CronExpressionScheduleAttribute"/>，该值按 <see cref="CronTriggerMisfireInstruction"/> 解释；
	/// 对于 <see cref="SimpleScheduleAttribute"/>，按 <see cref="SimpleTriggerMisfireInstruction"/> 解释；
	/// 对于 <see cref="CalendarIntervalScheduleAttribute"/>，按 <see cref="CalendarIntervalTriggerMisfireInstruction"/> 解释；
	/// 对于 <see cref="DailyTimeIntervalScheduleAttribute"/>，按 <see cref="DailyTimeIntervalTriggerMisfireInstruction"/> 解释。</para>
	/// <para><c>-1</c>（<c>IgnoreMisfires</c>）对每种调度类型均有效。</para>
	/// </summary>
	/// <remarks>
	/// 默认值：<see langword="null"/>，即保持调度自身的智能策略不变。
	/// </remarks>
	public virtual int? MisfirePolicy { get; set; }

	/// <summary>
	/// 配置触发器构建器。
	/// </summary>
	/// <returns>由派生特性构建的 Quartz 调度构建器，用于创建触发器。</returns>
	public abstract IScheduleBuilder Configure();
}
