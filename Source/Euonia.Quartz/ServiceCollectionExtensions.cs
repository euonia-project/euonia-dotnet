using System.Globalization;
using Nerosoft.Euonia.Quartz;
using Quartz;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// 用于在 <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" /> 中设置 Quartz 服务的扩展方法。
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// 将 Quartz 服务添加到指定的 <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />。
	/// </summary>
	/// <param name="services">要添加 Quartz 服务的服务集合。</param>
	/// <param name="options">用于配置 <see cref="QuartzOptions"/> 的委托，会注册到服务集合。</param>
	/// <param name="configure">直接用于配置 <see cref="IQuartzBuilder"/> 的委托。</param>
	/// <returns>同一个 <paramref name="services"/> 实例，以便链式调用。</returns>
	/// <remarks>
	/// 该重载直接使用 <paramref name="configure"/> 配置构建器；托管服务相关选项取自
	/// <see cref="Singleton{T}"/> 中共享的 <see cref="BackgroundBuildOptions"/> 实例，
	/// 不会应用该实例上的作业、扁平属性与构建器配置集合。
	/// </remarks>
	public static IServiceCollection AddQuartz(this IServiceCollection services, Action<QuartzOptions> options, Action<IQuartzBuilder> configure)
	{
		var buildOptions = Singleton<BackgroundBuildOptions>.Get(() => new BackgroundBuildOptions());

		services.Configure(options);
		services.AddQuartz(configure);
		services.AddQuartzHostedService(ConfigureHostedService(buildOptions));
		return services;
	}

	/// <summary>
	/// 将 Quartz 服务添加到指定的 <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />。
	/// </summary>
	/// <param name="services">要添加 Quartz 服务的服务集合。</param>
	/// <param name="optionsAction">用于配置共享 <see cref="BackgroundBuildOptions"/> 的委托；为 <see langword="null"/> 时使用默认选项。</param>
	/// <returns>同一个 <paramref name="services"/> 实例，以便链式调用。</returns>
	/// <remarks>
	/// <para>注册时会依次应用扁平属性、调度器选项、构建器配置操作与已注册的作业，并添加 Quartz 托管服务。</para>
	/// <para><see cref="BackgroundBuildOptions.MisfireThreshold"/> 会被转换为 <c>quartz.jobStore.misfireThreshold</c> 属性，以便对之后配置的任意作业存储生效。</para>
	/// </remarks>
	public static IServiceCollection AddQuartz(this IServiceCollection services, Action<BackgroundBuildOptions>? optionsAction = null)
	{
		var buildOptions = Singleton<BackgroundBuildOptions>.Get(() => new BackgroundBuildOptions());
		optionsAction?.Invoke(buildOptions);

		// Quartz 4 将失火阈值合并到作业存储选项中，而扁平属性会在构建器回调运行
		// 之前被桥接；将其作为属性提供，可确保它对用户稍后配置的任意存储
		//（内存或持久化）都仍然生效。
		var properties = new Dictionary<string, string?>(buildOptions.Properties, StringComparer.OrdinalIgnoreCase);
		if (buildOptions.MisfireThreshold.HasValue && !properties.ContainsKey("quartz.jobStore.misfireThreshold"))
		{
			properties["quartz.jobStore.misfireThreshold"] = ((long)buildOptions.MisfireThreshold.Value.TotalMilliseconds)
				.ToString(CultureInfo.InvariantCulture);
		}

		services.AddQuartz(properties, quartz =>
		{
			quartz.ConfigureOptions<QuartzOptions>(options =>
			{
				// Quartz 4 拒绝同时声明这两个标志。覆盖开启时优先；
				// 否则 IgnoreDuplicates（开启时）会关闭覆盖，并且只有两者都
				// 关闭时，重复键才会成为错误。
				if (buildOptions.OverWriteExistingData)
				{
					options.Scheduling.OverwriteExistingData = true;
				}
				else if (buildOptions.IgnoreDuplicates)
				{
					options.Scheduling.IgnoreDuplicates = true;
				}
				else
				{
					options.Scheduling.OverwriteExistingData = false;
				}
			});

			quartz.ConfigureScheduler(scheduler => ApplySchedulerOptions(scheduler, buildOptions));

			foreach (var action in buildOptions.Configurations)
			{
				action(quartz);
			}

			foreach (var (type, config) in buildOptions.Jobs)
			{
				quartz.AddJob(type, config);
			}
		});
		services.AddQuartzHostedService(ConfigureHostedService(buildOptions));
		return services;
	}

	private static Action<QuartzHostedServiceOptions> ConfigureHostedService(BackgroundBuildOptions buildOptions)
	{
		return host =>
		{
			host.WaitForJobsToComplete = buildOptions.WaitForJobsToComplete;
			host.AwaitApplicationStarted = buildOptions.AwaitApplicationStarted;

			if (buildOptions.StartDelay.HasValue)
			{
				host.StartDelay = buildOptions.StartDelay.Value;
			}
		};
	}

	private static void ApplySchedulerOptions(QuartzSchedulerOptions scheduler, BackgroundBuildOptions buildOptions)
	{
		if (!string.IsNullOrWhiteSpace(buildOptions.SchedulerId))
		{
			// Quartz 3 将 AUTO/SYS_PROP 视为“在启动时生成 id”；Quartz 4 将其改为
			// 一个独立的开关，而不再接受该哨兵值本身作为 id。
			if (buildOptions.SchedulerId is "AUTO" or "SYS_PROP")
			{
				scheduler.GenerateInstanceId = true;
			}
			else
			{
				scheduler.InstanceId = buildOptions.SchedulerId;
			}
		}

		if (!string.IsNullOrWhiteSpace(buildOptions.SchedulerName))
		{
			scheduler.InstanceName = buildOptions.SchedulerName;
		}

		if (buildOptions.MaxBatchSize.HasValue)
		{
			scheduler.MaxBatchSize = buildOptions.MaxBatchSize.Value;
		}

		// Quartz 3 暴露了两个独立的标志；Quartz 4 用一个枚举来回答同样的四选一问题。
		// 仅当调用方确实表达了其中一个标志时才覆盖默认值。
		if (buildOptions.InterruptJobsOnShutdown.HasValue || buildOptions.InterruptJobsOnShutdownWithWait.HasValue)
		{
			var interrupt = buildOptions.InterruptJobsOnShutdown ?? false;
			var interruptWithWait = buildOptions.InterruptJobsOnShutdownWithWait ?? false;

			scheduler.ShutdownJobInterruption = (interrupt, interruptWithWait) switch
			{
				(true, true) => ShutdownJobInterruption.Always,
				(true, false) => ShutdownJobInterruption.WhenNotWaitingForJobs,
				(false, true) => ShutdownJobInterruption.WhenWaitingForJobs,
				_ => ShutdownJobInterruption.Never
			};
		}
	}
}
