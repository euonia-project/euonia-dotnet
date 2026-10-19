using Nerosoft.Euonia.Quartz;
using Quartz;

// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedType.Global

namespace Microsoft.Extensions.DependencyInjection;

/// <summary>
/// Extension methods for setting up Quartz services in an <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
/// </summary>
public static class ServiceCollectionExtensions
{
	/// <summary>
	/// Adds Quartz services to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="options"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public static IServiceCollection AddQuartz(this IServiceCollection services, Action<QuartzOptions> options, Action<IServiceCollectionQuartzConfigurator> configure)
	{
		var buildOptions = Singleton<BackgroundBuildOptions>.Instance;

		services.Configure(options);
		services.AddQuartz(configure);
		services.AddQuartzHostedService(host =>
		{
			// 这些选项此前被硬编码，用户配置的 BackgroundBuildOptions 从未生效。
			host.WaitForJobsToComplete = buildOptions.WaitForJobsToComplete;
			host.AwaitApplicationStarted = buildOptions.AwaitApplicationStarted;

			if (buildOptions.StartDelay.HasValue)
			{
				host.StartDelay = buildOptions.StartDelay.Value;
			}
		});
		return services;
	}

	/// <summary>
	/// Adds Quartz services to the specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
	/// </summary>
	/// <param name="services"></param>
	/// <param name="optionsAction"></param>
	/// <returns></returns>
	public static IServiceCollection AddQuartz(this IServiceCollection services, Action<BackgroundBuildOptions> optionsAction = null)
	{
		var buildOptions = Singleton<BackgroundBuildOptions>.Instance;
		optionsAction?.Invoke(buildOptions);

		services.Configure<QuartzOptions>(options =>
		{
			options.Scheduling.IgnoreDuplicates = buildOptions.IgnoreDuplicates;
			options.Scheduling.OverWriteExistingData = buildOptions.OverWriteExistingData;
			options.MisfireThreshold = buildOptions.MisfireThreshold;
		});

		services.AddQuartz(quartz =>
		{
			quartz.SchedulerId = buildOptions.SchedulerId;
			quartz.SchedulerName = buildOptions.SchedulerName;

			/*
			if (buildOptions.TypeLoader != null)
			{
				Reflect.InvokeGenericMethod(quartz, nameof(quartz.UseTypeLoader), new[] { buildOptions.TypeLoader });
			}
			else
			{
				quartz.UseSimpleTypeLoader();
			}
			*/

			if (buildOptions.MaxBatchSize.HasValue)
			{
				quartz.MaxBatchSize = buildOptions.MaxBatchSize.Value;
			}

			if (buildOptions.InterruptJobsOnShutdown.HasValue)
			{
				quartz.InterruptJobsOnShutdown = buildOptions.InterruptJobsOnShutdown.Value;
			}

			if (buildOptions.InterruptJobsOnShutdownWithWait.HasValue)
			{
				quartz.InterruptJobsOnShutdownWithWait = buildOptions.InterruptJobsOnShutdownWithWait.Value;
			}

			foreach (var action in buildOptions.Configurations)
			{
				action(quartz);
			}

			foreach (var (type, config) in buildOptions.Jobs)
			{
				quartz.AddJob(type, config);
			}
		});
		services.AddQuartzHostedService(host =>
		{
			// 这些选项此前被硬编码，用户配置的 BackgroundBuildOptions 从未生效。
			host.WaitForJobsToComplete = buildOptions.WaitForJobsToComplete;
			host.AwaitApplicationStarted = buildOptions.AwaitApplicationStarted;

			if (buildOptions.StartDelay.HasValue)
			{
				host.StartDelay = buildOptions.StartDelay.Value;
			}
		});
		return services;
	}
}