using System.Diagnostics.CodeAnalysis;
using Quartz;
using Quartz.Extensibility;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// 后台构建选项。
/// </summary>
[SuppressMessage("ReSharper", "MemberCanBePrivate.Global")]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public class BackgroundBuildOptions
{
	/// <summary>
	/// 获取作业集合。
	/// </summary>
	internal Dictionary<Type, BackgroundJobOptions?> Jobs { get; } = new();

	/// <summary>
	/// 用于配置 <see cref="IQuartzBuilder"/> 的操作集合。
	/// </summary>
	internal List<Action<IQuartzBuilder>> Configurations { get; } = new();

	/// <summary>
	/// 在注册调度器之前应用的扁平 <c>quartz.*</c> 属性。
	/// </summary>
	internal Dictionary<string, string?> Properties { get; } = new(StringComparer.OrdinalIgnoreCase);

	/// <summary>
	/// 获取或设置调度器实例标识。
	/// </summary>
	/// <remarks>
	/// 值为 <c>AUTO</c> 或 <c>SYS_PROP</c> 时改为在启动时自动生成实例标识；为
	/// <see langword="null" /> 或空白时保留 Quartz 的默认行为。
	/// </remarks>
	public string? SchedulerId { get; set; }

	/// <summary>
	/// 获取或设置调度器实例名称。
	/// </summary>
	/// <remarks>
	/// 为 <see langword="null" /> 或空白时保留 Quartz 的默认实例名称。
	/// </remarks>
	public string? SchedulerName { get; set; }

	/// <summary>
	/// 获取或设置作业触发被判定为失火的时间阈值。
	/// </summary>
	/// <remarks>
	/// 该值以毫秒为单位写入 <c>quartz.jobStore.misfireThreshold</c>；为
	/// <see langword="null" /> 时使用 Quartz 的默认值。
	/// </remarks>
	public TimeSpan? MisfireThreshold { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示是否忽略具有相同标识的重复调度数据。
	/// </summary>
	/// <remarks>
	/// 默认值为 <see langword="true" />，且仅在 <see cref="OverWriteExistingData"/> 为
	/// <see langword="false" /> 时生效；两者均为 <see langword="false" /> 时，重复的键会被视为错误。
	/// </remarks>
	public bool IgnoreDuplicates { get; set; } = true;

	/// <summary>
	/// 获取或设置一个值，指示是否覆盖具有相同标识的现有调度数据。
	/// </summary>
	/// <remarks>
	/// 默认值为 <see langword="true" />；为 <see langword="true" /> 时优先于
	/// <see cref="IgnoreDuplicates"/> 生效。
	/// </remarks>
	public bool OverWriteExistingData { get; set; } = true;

	/// <summary>
	/// 获取或设置一个值，指示调度器关闭时是否中断正在执行的作业。
	/// </summary>
	/// <remarks>
	/// 为 <see langword="null" /> 时不覆盖 Quartz 的默认行为；该值与
	/// <see cref="InterruptJobsOnShutdownWithWait"/> 共同决定调度器的关闭中断策略。
	/// </remarks>
	public bool? InterruptJobsOnShutdown { get; set; }

	/// <summary>
	/// 获取或设置一个值，指示在等待作业完成而关闭时是否中断正在执行的作业。
	/// </summary>
	/// <remarks>
	/// 为 <see langword="null" /> 时不覆盖 Quartz 的默认行为；仅在
	/// <see cref="WaitForJobsToComplete"/> 为 <see langword="true" /> 时具有实际意义。
	/// </remarks>
	public bool? InterruptJobsOnShutdownWithWait { get; set; }

	/// <summary>
	/// 获取或设置调度器在单次批处理中允许获取的最大触发器数量。
	/// </summary>
	/// <remarks>
	/// 为 <see langword="null" /> 时使用 Quartz 的默认值。
	/// </remarks>
	public int? MaxBatchSize { get; set; }

	/// <summary>
	/// 获取或设置自定义类型加载器的实现类型。
	/// </summary>
	/// <remarks>
	/// 此属性仅保存类型，注册时不会被自动应用；如需向 Quartz 注册类型加载器，
	/// 请调用 <see cref="UseTypeLoader{T}"/>。
	/// </remarks>
	public Type? TypeLoader { get; set; }

	/// <summary>
	/// 如果为 <see langword="true" />，调度器将不会允许关闭过程
	/// 返回，直到当前正在执行的所有作业都已完成为止。
	/// </summary>
	public bool WaitForJobsToComplete { get; set; }

	/// <summary>
	/// <para>
	/// 如果不为 <see langword="null" />，调度器将在指定的延迟之后启动。
	/// </para>
	/// <para>
	/// 如果 <see cref="AwaitApplicationStarted"/> 为 true，则延迟从应用程序启动完成时开始计算。
	/// </para>
	/// </summary>
	public TimeSpan? StartDelay { get; set; }

	/// <summary>
	/// 如果为 true（默认值），作业在应用程序启动完成之前不会启动。
	/// 这样可以避免在应用程序启动<em>期间</em>运行作业。
	/// </summary>
	public bool AwaitApplicationStarted { get; set; } = true;

	/// <summary>
	/// 注册指定类型的作业，不附加作业选项。
	/// </summary>
	/// <typeparam name="TJob">要注册的作业类型。</typeparam>
	/// <remarks>
	/// 同一作业类型重复注册时，后一次注册会覆盖先前的记录，包括先前设置的作业选项。
	/// </remarks>
	public void AddJob<TJob>()
		where TJob : IJob
	{
		AddJob<TJob>(options: null);
	}

	/// <summary>
	/// 使用指定的作业选项注册类型为 <typeparamref name="TJob"/> 的作业。
	/// </summary>
	/// <typeparam name="TJob">要注册的作业类型。</typeparam>
	/// <param name="options">作业选项；为 <see langword="null" /> 时表示使用默认配置。</param>
	public void AddJob<TJob>(BackgroundJobOptions? options)
		where TJob : IJob
	{
		Jobs[typeof(TJob)] = options;
	}

	/// <summary>
	/// 通过配置委托注册类型为 <typeparamref name="TJob"/> 的作业。
	/// </summary>
	/// <typeparam name="TJob">要注册的作业类型。</typeparam>
	/// <param name="action">用于填充作业选项的委托；为 <see langword="null" /> 时不进行任何配置。</param>
	public void AddJob<TJob>(Action<BackgroundJobOptions> action)
		where TJob : IJob
	{
		var options = new BackgroundJobOptions();
		action?.Invoke(options);
		AddJob<TJob>(options);
	}

	/// <summary>
	/// 注册指定类型的作业，不附加作业选项。
	/// </summary>
	/// <param name="jobType">要注册的作业类型，通常应实现 <see cref="IJob"/>。</param>
	public void AddJob(Type jobType)
	{
		AddJob(jobType, options: null);
	}

	/// <summary>
	/// 使用指定的作业选项注册作业。
	/// </summary>
	/// <param name="jobType">要注册的作业类型，通常应实现 <see cref="IJob"/>。</param>
	/// <param name="options">作业选项；为 <see langword="null" /> 时表示使用默认配置。</param>
	public void AddJob(Type jobType, BackgroundJobOptions? options)
	{
		Jobs[jobType] = options;
	}

	/// <summary>
	/// 通过配置委托注册作业。
	/// </summary>
	/// <param name="jobType">要注册的作业类型，通常应实现 <see cref="IJob"/>。</param>
	/// <param name="action">用于填充作业选项的委托；为 <see langword="null" /> 时不进行任何配置。</param>
	public void AddJob(Type jobType, Action<BackgroundJobOptions> action)
	{
		var options = new BackgroundJobOptions();
		action?.Invoke(options);
		AddJob(jobType, options);
	}

	/// <summary>
	/// 添加一项针对 <see cref="IQuartzBuilder"/> 的配置操作。
	/// </summary>
	/// <param name="action">对 <see cref="IQuartzBuilder"/> 进行配置的委托，在构建调度器时按添加顺序执行。</param>
	/// <returns>当前 <see cref="BackgroundBuildOptions"/> 实例，用于链式调用。</returns>
	public BackgroundBuildOptions Configure(Action<IQuartzBuilder> action)
	{
		Configurations.Add(action);
		return this;
	}

	/// <summary>
	/// 使用 Quartz 提供的简单类型加载器。
	/// </summary>
	/// <returns>当前 <see cref="BackgroundBuildOptions"/> 实例，用于链式调用。</returns>
	public BackgroundBuildOptions UseSimpleTypeLoader()
	{
		Configurations.Add(cfg => cfg.UseSimpleTypeLoader());
		return this;
	}

	/// <summary>
	/// 使用 <typeparamref name="T"/> 作为类型加载器。
	/// </summary>
	/// <typeparam name="T">类型加载器实现类型，必须实现 <see cref="ITypeLoader"/>。</typeparam>
	/// <returns>当前 <see cref="BackgroundBuildOptions"/> 实例，用于链式调用。</returns>
	public BackgroundBuildOptions UseTypeLoader<T>()
		where T : class, ITypeLoader
	{
		Configurations.Add(cfg => cfg.UseTypeLoader<T>());
		return this;
	}

	/// <summary>
	/// 使用内存作业存储。
	/// </summary>
	/// <param name="configure">
	/// 用于配置 <see cref="InMemoryJobStoreOptions"/> 的委托；为 <see langword="null" /> 时使用默认配置。
	/// </param>
	/// <returns>当前 <see cref="BackgroundBuildOptions"/> 实例，用于链式调用。</returns>
	public BackgroundBuildOptions UseInMemoryStore(Action<InMemoryJobStoreOptions>? configure = null)
	{
		Configurations.Add(cfg => cfg.UseInMemoryStore(configure));
		return this;
	}

	/// <summary>
	/// 使用持久化作业存储。
	/// </summary>
	/// <param name="configure">用于配置持久化存储的委托；该委托会在构建调度器时执行。</param>
	/// <returns>当前 <see cref="BackgroundBuildOptions"/> 实例，用于链式调用。</returns>
	public BackgroundBuildOptions UsePersistentStore(Action<IPersistentStoreBuilder> configure)
	{
		Configurations.Add(cfg => cfg.UsePersistentStore(configure));
		return this;
	}

	/// <summary>
	/// 使用 <typeparamref name="T"/> 作为持久化作业存储。
	/// </summary>
	/// <typeparam name="T">作业存储实现类型，必须实现 <see cref="IJobStore"/>。</typeparam>
	/// <param name="configure">用于配置持久化存储的委托；该委托会在构建调度器时执行。</param>
	/// <returns>当前 <see cref="BackgroundBuildOptions"/> 实例，用于链式调用。</returns>
	public BackgroundBuildOptions UsePersistentStore<T>(Action<IPersistentStoreBuilder> configure)
		where T : class, IJobStore
	{
		Configurations.Add(cfg => cfg.UsePersistentStore<T>(configure));
		return this;
	}

	/// <summary>
	/// 使用 <typeparamref name="T"/> 作为线程池。
	/// </summary>
	/// <typeparam name="T">线程池实现类型，必须实现 <see cref="IThreadPool"/>。</typeparam>
	/// <param name="configure">
	/// 用于配置 <see cref="ThreadPoolOptions"/> 的委托；为 <see langword="null" /> 时不附加额外配置。
	/// </param>
	/// <returns>当前 <see cref="BackgroundBuildOptions"/> 实例，用于链式调用。</returns>
	public BackgroundBuildOptions UseThreadPool<T>(Action<ThreadPoolOptions>? configure = null)
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
	/// 使用默认线程池，并指定最大并发数。
	/// </summary>
	/// <param name="maxConcurrency">线程池允许的最大并发数，会覆盖 <see cref="ThreadPoolOptions.MaxConcurrency"/>。</param>
	/// <param name="configure">
	/// 在设置最大并发数之后执行的额外配置委托，可再次修改该值；为 <see langword="null" /> 时跳过。
	/// </param>
	/// <returns>当前 <see cref="BackgroundBuildOptions"/> 实例，用于链式调用。</returns>
	public BackgroundBuildOptions UseDefaultThreadPool(int maxConcurrency, Action<ThreadPoolOptions>? configure = null)
	{
		Configurations.Add(cfg => cfg.UseDefaultThreadPool(options =>
		{
			options.MaxConcurrency = maxConcurrency;
			configure?.Invoke(options);
		}));
		return this;
	}

	/// <summary>
	/// 使用默认线程池。
	/// </summary>
	/// <param name="configure">
	/// 用于配置默认线程池的委托；为 <see langword="null" /> 时使用 Quartz 的默认配置。
	/// </param>
	/// <returns>当前 <see cref="BackgroundBuildOptions"/> 实例，用于链式调用。</returns>
	public BackgroundBuildOptions UseDefaultThreadPool(Action<ThreadPoolOptions>? configure = null)
	{
		Configurations.Add(cfg => cfg.UseDefaultThreadPool(configure));
		return this;
	}

	/// <summary>
	/// 使用零大小线程池。
	/// </summary>
	/// <param name="configure">线程池配置委托；该方法始终抛出异常，因此不会使用此参数。</param>
	/// <returns>该方法不会正常返回，始终抛出 <see cref="NotSupportedException"/>。</returns>
	/// <exception cref="NotSupportedException">
	/// Quartz.NET 4.x 要求线程池至少包含一个线程；不存在零大小线程池。
	/// </exception>
	public BackgroundBuildOptions UseZeroSizeThreadPool(Action<ThreadPoolOptions>? configure = null)
	{
		throw new NotSupportedException("Quartz.NET 4.x no longer supports the zero size thread pool.");
	}

	/// <summary>
	/// 使用专用线程池，并指定最大并发数。
	/// </summary>
	/// <param name="maxConcurrency">线程池允许的最大并发数；该方法始终抛出异常，因此不会使用此参数。</param>
	/// <param name="configure">线程池配置委托；该方法始终抛出异常，因此不会使用此参数。</param>
	/// <returns>该方法不会正常返回，始终抛出 <see cref="NotSupportedException"/>。</returns>
	/// <exception cref="NotSupportedException">
	/// Quartz.NET 4.x 不再提供专用线程池；请改用 <see cref="UseDefaultThreadPool(int, Action{ThreadPoolOptions})"/>
	/// 或 <see cref="UseThreadPool{T}(Action{ThreadPoolOptions})"/>。
	/// </exception>
	public BackgroundBuildOptions UseDedicatedThreadPool(int maxConcurrency, Action<ThreadPoolOptions>? configure = null)
	{
		throw new NotSupportedException("Quartz.NET 4.x no longer supports the dedicated thread pool.");
	}

	/// <summary>
	/// 使用专用线程池。
	/// </summary>
	/// <param name="configure">线程池配置委托；该方法始终抛出异常，因此不会使用此参数。</param>
	/// <returns>该方法不会正常返回，始终抛出 <see cref="NotSupportedException"/>。</returns>
	/// <exception cref="NotSupportedException">
	/// Quartz.NET 4.x 不再提供专用线程池；请改用 <see cref="UseDefaultThreadPool(Action{ThreadPoolOptions})"/>
	/// 或 <see cref="UseThreadPool{T}(Action{ThreadPoolOptions})"/>。
	/// </exception>
	public BackgroundBuildOptions UseDedicatedThreadPool(Action<ThreadPoolOptions>? configure = null)
	{
		throw new NotSupportedException("Quartz.NET 4.x no longer supports the dedicated thread pool.");
	}

	/// <summary>
	/// 设置一个在注册调度器之前应用的扁平属性。
	/// </summary>
	/// <param name="name">属性名称，例如 <c>quartz.*</c>；比较时不区分大小写。</param>
	/// <param name="value">属性值。</param>
	/// <returns>当前 <see cref="BackgroundBuildOptions"/> 实例，用于链式调用。</returns>
	/// <exception cref="ArgumentNullException">
	/// <paramref name="name"/> 或 <paramref name="value"/> 为 <see langword="null" />。
	/// </exception>
	public BackgroundBuildOptions SetProperty(string name, string value)
	{
		ArgumentNullException.ThrowIfNull(name);
		ArgumentNullException.ThrowIfNull(value);

		Properties[name] = value;
		return this;
	}
}
