using System.Globalization;
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
	public static IServiceCollection AddQuartz(this IServiceCollection services, Action<QuartzOptions> options, Action<IQuartzBuilder> configure)
	{
		var buildOptions = Singleton<BackgroundBuildOptions>.Get(() => new BackgroundBuildOptions());

		services.Configure(options);
		services.AddQuartz(configure);
		services.AddQuartzHostedService(ConfigureHostedService(buildOptions));
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
		var buildOptions = Singleton<BackgroundBuildOptions>.Get(() => new BackgroundBuildOptions());
		optionsAction?.Invoke(buildOptions);

		// Quartz 4 folds the misfire threshold into the job store options, and the flat property is
		// bridged before the builder callback runs; feeding it as a property keeps it applying to
		// whichever store (in-memory or persistent) the user configures later.
		var properties = new Dictionary<string, string>(buildOptions.Properties, StringComparer.OrdinalIgnoreCase);
		if (buildOptions.MisfireThreshold.HasValue && !properties.ContainsKey("quartz.jobStore.misfireThreshold"))
		{
			properties["quartz.jobStore.misfireThreshold"] = ((long)buildOptions.MisfireThreshold.Value.TotalMilliseconds)
				.ToString(CultureInfo.InvariantCulture);
		}

		services.AddQuartz(properties, quartz =>
		{
			quartz.ConfigureOptions<QuartzOptions>(options =>
			{
				// Quartz 4 refuses both flags being stated at once. Overwrite wins when it is on;
				// otherwise IgnoreDuplicates (when on) turns overwriting off, and only when both are
				// off does a duplicate key become an error.
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
			// Quartz 3 treated AUTO/SYS_PROP as "generate the id at startup"; Quartz 4 moves that to
			// a separate switch instead of accepting the sentinel as the id itself.
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

		// Quartz 3 exposed two independent flags; Quartz 4 answers the same four-way question with one
		// enum. Only override the default when the caller actually expressed one of the flags.
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
