using System.Reflection;
using Quartz;

// ReSharper disable MemberCanBePrivate.Global

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 用于设置 Quartz 服务的扩展方法。
/// </summary>
public static class QuartzExtensions
{
	/// <summary>
	/// 注册指定类型的作业。
	/// </summary>
	/// <typeparam name="TJob">作业类型，必须实现 <see cref="IJob"/>。</typeparam>
	/// <param name="configurator">Quartz 构建器。</param>
	/// <param name="options">作业选项；为 <see langword="null"/> 时使用默认选项。</param>
	/// <returns>同一个 <paramref name="configurator"/> 实例，以便链式调用。</returns>
	public static IQuartzBuilder AddJob<TJob>(this IQuartzBuilder configurator, BackgroundJobOptions? options = null)
		where TJob : IJob
	{
		return configurator.AddJob(typeof(TJob), options);
	}

	/// <summary>
	/// 按类型注册作业及其触发器。
	/// </summary>
	/// <param name="configurator">Quartz 构建器。</param>
	/// <param name="jobType">作业类型，必须实现 <see cref="IJob"/>。</param>
	/// <param name="options">作业选项；为 <see langword="null"/> 时创建默认选项。</param>
	/// <returns>同一个 <paramref name="configurator"/> 实例，以便链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="jobType"/> 或 <paramref name="configurator"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="ArgumentException">当 <paramref name="jobType"/> 未实现 <see cref="IJob"/> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当类型上的调度特性与选项均未提供任何触发器配置时抛出。</exception>
	/// <remarks>
	/// <para>未指定名称或组时，依次使用 <see cref="BackgroundJobAttribute"/> 的值、类型名称和 <c>default</c> 组。</para>
	/// <para>作业键为 <c>{Name}.job</c>，触发器标识由 <see cref="BackgroundJobScheduleAttribute.Identity"/> 附加 <c>.trigger</c> 后缀构成。</para>
	/// </remarks>
	public static IQuartzBuilder AddJob(this IQuartzBuilder configurator, Type jobType, BackgroundJobOptions? options = null)
	{
		ArgumentNullException.ThrowIfNull(jobType);
		ArgumentNullException.ThrowIfNull(configurator);

		if (!typeof(IJob).IsAssignableFrom(jobType))
		{
			throw new ArgumentException(string.Format(Resources.IDS_TYPE_MUST_BE_JOB_TYPE, jobType.FullName));
		}

		var attribute = jobType.GetCustomAttribute<BackgroundJobAttribute>();

		var triggers = jobType.GetCustomAttributes<BackgroundJobScheduleAttribute>(true)
							  .ToList();

		options ??= new BackgroundJobOptions();
		if (string.IsNullOrWhiteSpace(options.Name))
		{
			options.Name = attribute?.Name ?? jobType.Name;
		}

		if (string.IsNullOrWhiteSpace(options.Group))
		{
			options.Group = attribute?.Group ?? "default";
		}

		options.JobConfigure ??= job =>
		{
			if (attribute == null)
			{
				return;
			}

			job.WithDescription(attribute.Description);
			if (attribute.RequestRecovery.HasValue)
			{
				job.RequestRecovery(attribute.RequestRecovery.Value);
			}

			if (attribute.StoreDurably.HasValue)
			{
				job.StoreDurably(attribute.StoreDurably.Value);
			}
		};

		var jobKey = new JobKey($"{options.Name}.job", options.Group);
		configurator.AddJob(jobType, job =>
		{
			job.WithIdentity(jobKey);
			options.JobConfigure(job);
		});

		foreach (var trigger in triggers)
		{
			options.TriggerConfigure.Add(config => config.Configure(trigger, jobKey));
		}

		if (options.TriggerConfigure.Count == 0)
		{
			throw new InvalidOperationException(Resources.IDS_NO_TRIGGER_CONFIGURATION);
		}

		foreach (var configure in options.TriggerConfigure)
		{
			configurator.AddTrigger(configure);
		}

		return configurator;
	}

	/// <summary>
	/// 注册指定程序集中所有实现 <see cref="IJob"/> 的导出类型。
	/// </summary>
	/// <param name="configurator">Quartz 构建器。</param>
	/// <param name="assembly">要扫描的程序集。</param>
	/// <returns>同一个 <paramref name="configurator"/> 实例，以便链式调用。</returns>
	/// <exception cref="ArgumentNullException">当 <paramref name="assembly"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">当程序集中的某个作业类型没有可用的触发器配置时抛出。</exception>
	public static IQuartzBuilder AddJobs(this IQuartzBuilder configurator, Assembly assembly)
	{
		ArgumentNullException.ThrowIfNull(assembly);

		var types = assembly.GetExportedTypes()
							.Where(type => typeof(IJob).IsAssignableFrom(type));
		foreach (var type in types)
		{
			configurator.AddJob(type);
		}

		return configurator;
	}

	/// <summary>
	/// 使用指定的调度特性配置触发器。
	/// </summary>
	/// <param name="config">触发器配置器。</param>
	/// <param name="attribute">提供触发器标识、调度、优先级和延迟的调度特性。</param>
	/// <param name="jobKey">触发器关联的作业键。</param>
	/// <returns>同一个 <paramref name="config"/> 实例，以便链式调用。</returns>
	/// <remarks>当 <see cref="BackgroundJobScheduleAttribute.Delay"/> 大于 0 时，在延迟结束后的最近偶数秒启动；否则立即启动。</remarks>
	public static ITriggerConfigurator<IJob> Configure(this ITriggerConfigurator<IJob> config, BackgroundJobScheduleAttribute attribute, JobKey jobKey)
	{
		config.WithIdentity($"{attribute.Identity}.trigger")
			  .ForJob(jobKey)
			  .WithSchedule(attribute.Configure())
			  .WithPriority(attribute.Priority)
			  .WithDescription(attribute.Description);
		if (attribute.Delay > 0)
		{
			config.StartAt(EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(attribute.Delay)));
		}
		else
		{
			config.StartNow();
		}

		return config;
	}

	/// <summary>
	/// 使用指定具体类型的调度特性配置触发器。
	/// </summary>
	/// <typeparam name="TSchedule">调度特性的具体类型，必须继承 <see cref="BackgroundJobScheduleAttribute"/>。</typeparam>
	/// <param name="config">触发器配置器。</param>
	/// <param name="attribute">提供触发器标识、调度、优先级和延迟的调度特性。</param>
	/// <param name="jobKey">触发器关联的作业键。</param>
	/// <returns>同一个 <paramref name="config"/> 实例，以便链式调用。</returns>
	/// <remarks>当 <see cref="BackgroundJobScheduleAttribute.Delay"/> 大于 0 时，在延迟结束后的最近偶数秒启动；否则立即启动。</remarks>
	public static ITriggerConfigurator<IJob> Configure<TSchedule>(this ITriggerConfigurator<IJob> config, TSchedule attribute, JobKey jobKey)
		where TSchedule : BackgroundJobScheduleAttribute
	{
		config.WithIdentity($"{attribute.Identity}.trigger")
			  .ForJob(jobKey)
			  .WithSchedule(attribute.Configure())
			  .WithPriority(attribute.Priority)
			  .WithDescription(attribute.Description);
		if (attribute.Delay > 0)
		{
			config.StartAt(EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(attribute.Delay)));
		}
		else
		{
			config.StartNow();
		}

		return config;
	}

	/// <summary>
	/// 将指定时间向上舍入到下一个偶数秒。
	/// </summary>
	private static DateTimeOffset EvenSecondDate(DateTimeOffset value)
	{
		var seconds = value.Ticks - value.Ticks % TimeSpan.TicksPerSecond;
		var truncated = new DateTimeOffset(seconds, value.Offset);
		return truncated.Second % 2 == 0
			? truncated
			: truncated.AddSeconds(1);
	}
}
