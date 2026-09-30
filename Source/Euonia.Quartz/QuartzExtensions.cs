using System.Reflection;
using Quartz;

// ReSharper disable MemberCanBePrivate.Global

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// Extension methods for setting up Quartz services.
/// </summary>
public static class QuartzExtensions
{
	/// <summary>
	/// 
	/// </summary>
	/// <param name="configurator"></param>
	/// <param name="options"></param>
	/// <typeparam name="TJob"></typeparam>
	/// <returns></returns>
	public static IServiceCollectionQuartzConfigurator AddJob<TJob>(this IServiceCollectionQuartzConfigurator configurator, BackgroundJobOptions options = null)
		where TJob : IJob
	{
		return configurator.AddJob(typeof(TJob), options);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="configurator"></param>
	/// <param name="jobType"></param>
	/// <param name="options"></param>
	/// <returns></returns>
	/// <exception cref="ArgumentNullException"></exception>
	/// <exception cref="ArgumentException"></exception>
	public static IServiceCollectionQuartzConfigurator AddJob(this IServiceCollectionQuartzConfigurator configurator, Type jobType, BackgroundJobOptions options = null)
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
		configurator.AddJob(jobType, jobKey, job => options.JobConfigure(job));

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
	/// Add jobs from the specified assembly.
	/// </summary>
	/// <param name="configurator"></param>
	/// <param name="assembly"></param>
	/// <returns></returns>
	public static IServiceCollectionQuartzConfigurator AddJobs(this IServiceCollectionQuartzConfigurator configurator, Assembly assembly)
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
	/// Configure the job trigger.
	/// </summary>
	/// <param name="config"></param>
	/// <param name="attribute"></param>
	/// <param name="jobKey"></param>
	/// <returns></returns>
	public static ITriggerConfigurator Configure(this ITriggerConfigurator config, BackgroundJobScheduleAttribute attribute, JobKey jobKey)
	{
		config.WithIdentity($"{attribute.Identity}.trigger")
			  .ForJob(jobKey)
			  .WithSchedule(attribute.Configure())
			  .WithPriority(attribute.Priority)
			  .WithDescription(attribute.Description);
		if (attribute.Delay > 0)
		{
			config.StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(attribute.Delay)));
		}
		else
		{
			config.StartNow();
		}

		return config;
	}

	/// <summary>
	/// Configure the job trigger.
	/// </summary>
	/// <param name="config"></param>
	/// <param name="attribute"></param>
	/// <param name="jobKey"></param>
	/// <typeparam name="TSchedule"></typeparam>
	/// <returns></returns>
	public static ITriggerConfigurator Configure<TSchedule>(this ITriggerConfigurator config, TSchedule attribute, JobKey jobKey)
		where TSchedule : BackgroundJobScheduleAttribute
	{
		config.WithIdentity($"{attribute.Identity}.trigger")
			  .ForJob(jobKey)
			  .WithSchedule(attribute.Configure())
			  .WithPriority(attribute.Priority)
			  .WithDescription(attribute.Description);
		if (attribute.Delay > 0)
		{
			config.StartAt(DateBuilder.EvenSecondDate(DateTimeOffset.UtcNow.AddSeconds(attribute.Delay)));
		}
		else
		{
			config.StartNow();
		}

		return config;
	}
}