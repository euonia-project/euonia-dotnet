using System.Diagnostics.CodeAnalysis;
using Quartz;
using Quartz.Extensibility;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// The background build options.
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class BackgroundBuildOptions
{
	/// <summary>
	/// Gets the jobs.
	/// </summary>
	internal Dictionary<Type, BackgroundJobOptions> Jobs { get; } = new();

	/// <summary>
	/// The actions to configure the <see cref="IQuartzBuilder"/>.
	/// </summary>
	internal List<Action<IQuartzBuilder>> Configurations { get; } = new();

	/// <summary>
	/// The flat <c>quartz.*</c> properties applied before the scheduler is registered.
	/// </summary>
	internal Dictionary<string, string> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// Gets or sets the scheduler identifier.
	/// </summary>
	public string SchedulerId { get; set; }

	/// <summary>
	/// Gets or sets the scheduler name.
	/// </summary>
	public string SchedulerName { get; set; }

	/// <summary>
	/// Gets or sets the misfire threshold time.
	/// </summary>
	public TimeSpan? MisfireThreshold { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public bool IgnoreDuplicates { get; set; } = true;

	/// <summary>
	/// Gets or sets a value indicating whether the existing scheduling data (with same identifiers) will be overwritten.
	/// </summary>
	public bool OverWriteExistingData { get; set; } = true;

	/// <summary>
	/// 
	/// </summary>
	public bool? InterruptJobsOnShutdown { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public bool? InterruptJobsOnShutdownWithWait { get; set; }

	/// <summary>
	/// 
	/// </summary>
	public int? MaxBatchSize { get; set; }

	/// <summary>
	/// Gets or sets the type loader.
	/// </summary>
	public Type TypeLoader { get; set; }

	/// <summary>
	/// If <see langword="true" /> the scheduler will not allow shutdown process
	/// to return until all currently executing jobs have completed.
	/// </summary>
	public bool WaitForJobsToComplete { get; set; }

	/// <summary>
	/// <para>
	/// If not <see langword="null" /> the scheduler will start after specified delay.
	/// </para>
	/// <para>
	/// If <see cref="AwaitApplicationStarted"/> is true, the delay starts when application startup completes.
	/// </para>
	/// </summary>
	public TimeSpan? StartDelay { get; set; }

	/// <summary>
	/// If true (default), jobs will not be started until application startup completes.
	/// This avoids the running of jobs <em>during</em> application startup.
	/// </summary>
	public bool AwaitApplicationStarted { get; set; } = true;

	/// <summary>
	/// Adds the job.
	/// </summary>
	/// <typeparam name="TJob"></typeparam>
	public void AddJob<TJob>()
		where TJob : IJob
	{
		AddJob<TJob>(options: null);
	}

	/// <summary>
	/// Adds the job.
	/// </summary>
	/// <param name="options"></param>
	/// <typeparam name="TJob"></typeparam>
	public void AddJob<TJob>(BackgroundJobOptions options)
		where TJob : IJob
	{
		Jobs[typeof(TJob)] = options;
	}

	/// <summary>
	/// Adds the job.
	/// </summary>
	/// <param name="action"></param>
	/// <typeparam name="TJob"></typeparam>
	public void AddJob<TJob>(Action<BackgroundJobOptions> action)
		where TJob : IJob
	{
		var options = new BackgroundJobOptions();
		action?.Invoke(options);
		AddJob<TJob>(options);
	}

	/// <summary>
	/// Adds the job.
	/// </summary>
	/// <param name="jobType"></param>
	public void AddJob(Type jobType)
	{
		AddJob(jobType, options: null);
	}

	/// <summary>
	/// Adds the job.
	/// </summary>
	/// <param name="jobType"></param>
	/// <param name="options"></param>
	public void AddJob(Type jobType, BackgroundJobOptions options)
	{
		Jobs[jobType] = options;
	}

	/// <summary>
	/// Adds the job.
	/// </summary>
	public void AddJob(Type jobType, Action<BackgroundJobOptions> action)
	{
		var options = new BackgroundJobOptions();
		action?.Invoke(options);
		AddJob(jobType, options);
	}

	/// <summary>
	/// Configures the scheduler.
	/// </summary>
	/// <param name="action"></param>
	/// <returns></returns>
	public BackgroundBuildOptions Configure(Action<IQuartzBuilder> action)
	{
		Configurations.Add(action);
		return this;
	}

	/// <summary>
	/// Use simple type loader.
	/// </summary>
	/// <returns></returns>
	public BackgroundBuildOptions UseSimpleTypeLoader()
	{
		Configurations.Add(cfg => cfg.UseSimpleTypeLoader());
		return this;
	}

	/// <summary>
	/// Use type loader of <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public BackgroundBuildOptions UseTypeLoader<T>()
		where T : class, ITypeLoader
	{
		Configurations.Add(cfg => cfg.UseTypeLoader<T>());
		return this;
	}

	/// <summary>
	/// Use in-memory store.
	/// </summary>
	/// <param name="configure"></param>
	/// <returns></returns>
	public BackgroundBuildOptions UseInMemoryStore(Action<InMemoryJobStoreOptions> configure = null)
	{
		Configurations.Add(cfg => cfg.UseInMemoryStore(configure));
		return this;
	}

	/// <summary>
	/// Use persistent store.
	/// </summary>
	/// <param name="configure"></param>
	/// <returns></returns>
	public BackgroundBuildOptions UsePersistentStore(Action<IPersistentStoreBuilder> configure)
	{
		Configurations.Add(cfg => cfg.UsePersistentStore(configure));
		return this;
	}

	/// <summary>
	/// Use persistent store of <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="configure"></param>
	/// <returns></returns>
	public BackgroundBuildOptions UsePersistentStore<T>(Action<IPersistentStoreBuilder> configure)
		where T : class, IJobStore
	{
		Configurations.Add(cfg => cfg.UsePersistentStore<T>(configure));
		return this;
	}

	/// <summary>
	/// Use thread pool of <typeparamref name="T"/>.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <param name="configure"></param>
	/// <returns></returns>
	public BackgroundBuildOptions UseThreadPool<T>(Action<ThreadPoolOptions> configure = null)
		where T : class, IThreadPool
	{
		Configurations.Add(cfg =>
		{
			if (configure != null)
			{
				cfg.ConfigureOptions<ThreadPoolOptions>(configure);
			}

			cfg.UseThreadPool<T>();
		});
		return this;
	}

	/// <summary>
	/// Use default thread pool.
	/// </summary>
	/// <param name="maxConcurrency"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	public BackgroundBuildOptions UseDefaultThreadPool(int maxConcurrency, Action<ThreadPoolOptions> configure = null)
	{
		Configurations.Add(cfg => cfg.UseDefaultThreadPool(options =>
		{
			options.MaxConcurrency = maxConcurrency;
			configure?.Invoke(options);
		}));
		return this;
	}

	/// <summary>
	/// Use default thread pool.
	/// </summary>
	/// <param name="configure"></param>
	/// <returns></returns>
	public BackgroundBuildOptions UseDefaultThreadPool(Action<ThreadPoolOptions> configure = null)
	{
		Configurations.Add(cfg => cfg.UseDefaultThreadPool(configure));
		return this;
	}

	/// <summary>
	/// Use zero size thread pool.
	/// </summary>
	/// <param name="configure"></param>
	/// <returns></returns>
	/// <exception cref="NotSupportedException">
	/// Quartz.NET 4.x requires a thread pool with at least one thread; there is no zero-size pool.
	/// </exception>
	public BackgroundBuildOptions UseZeroSizeThreadPool(Action<ThreadPoolOptions> configure = null)
	{
		throw new NotSupportedException("Quartz.NET 4.x no longer supports the zero size thread pool.");
	}

	/// <summary>
	/// Use dedicated thread pool.
	/// </summary>
	/// <param name="maxConcurrency"></param>
	/// <param name="configure"></param>
	/// <returns></returns>
	/// <exception cref="NotSupportedException">
	/// Quartz.NET 4.x no longer ships a dedicated thread pool; use <see cref="UseDefaultThreadPool(int, Action{ThreadPoolOptions})"/>
	/// or <see cref="UseThreadPool{T}(Action{ThreadPoolOptions})"/> instead.
	/// </exception>
	public BackgroundBuildOptions UseDedicatedThreadPool(int maxConcurrency, Action<ThreadPoolOptions> configure = null)
	{
		throw new NotSupportedException("Quartz.NET 4.x no longer supports the dedicated thread pool.");
	}

	/// <summary>
	/// Use dedicated thread pool.
	/// </summary>
	/// <param name="configure"></param>
	/// <returns></returns>
	/// <exception cref="NotSupportedException">
	/// Quartz.NET 4.x no longer ships a dedicated thread pool; use <see cref="UseDefaultThreadPool(Action{ThreadPoolOptions})"/>
	/// or <see cref="UseThreadPool{T}(Action{ThreadPoolOptions})"/> instead.
	/// </exception>
	public BackgroundBuildOptions UseDedicatedThreadPool(Action<ThreadPoolOptions> configure = null)
	{
		throw new NotSupportedException("Quartz.NET 4.x no longer supports the dedicated thread pool.");
	}

	/// <summary>
	/// Set property.
	/// </summary>
	/// <param name="name"></param>
	/// <param name="value"></param>
	/// <returns></returns>
	public BackgroundBuildOptions SetProperty(string name, string value)
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(value);

		Properties[name] = value;
		return this;
	}
}
