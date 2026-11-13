# Euonia.Quartz 完整 API 参考

> 由程序集公共元数据生成，覆盖本包全部公开类型与公共成员签名。
> 语义、用法和配置说明见 [API.md](API.md)；端到端示例见 [TUTORIAL.md](TUTORIAL.md)。

| 项 | 值 |
|---|---|
| 程序集 | Euonia.Quartz |
| 公共类型 | 13 |
| 命名空间 | 2 |

## 命名空间 Microsoft.Extensions.DependencyInjection

### Microsoft.Extensions.DependencyInjection.ServiceCollectionExtensions

**类型**：static class

- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddQuartz(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Quartz.QuartzOptions> options, Action<Quartz.IQuartzBuilder> configure)
- static Microsoft.Extensions.DependencyInjection.IServiceCollection AddQuartz(Microsoft.Extensions.DependencyInjection.IServiceCollection services, Action<Nerosoft.Euonia.Quartz.BackgroundBuildOptions> optionsAction = null)

## 命名空间 Nerosoft.Euonia.Quartz

### Nerosoft.Euonia.Quartz.BackgroundBuildOptions

**类型**：class

- new Nerosoft.Euonia.Quartz.BackgroundBuildOptions()
- System.Boolean AwaitApplicationStarted { get; set; }
- System.Boolean IgnoreDuplicates { get; set; }
- Nullable<System.Boolean> InterruptJobsOnShutdown { get; set; }
- Nullable<System.Boolean> InterruptJobsOnShutdownWithWait { get; set; }
- Nullable<System.Int32> MaxBatchSize { get; set; }
- Nullable<System.TimeSpan> MisfireThreshold { get; set; }
- System.Boolean OverWriteExistingData { get; set; }
- System.String SchedulerId { get; set; }
- System.String SchedulerName { get; set; }
- Nullable<System.TimeSpan> StartDelay { get; set; }
- System.Type TypeLoader { get; set; }
- System.Boolean WaitForJobsToComplete { get; set; }
- void AddJob<TJob>()
- void AddJob<TJob>(Nerosoft.Euonia.Quartz.BackgroundJobOptions options)
- void AddJob<TJob>(Action<Nerosoft.Euonia.Quartz.BackgroundJobOptions> action)
- void AddJob(System.Type jobType)
- void AddJob(System.Type jobType, Nerosoft.Euonia.Quartz.BackgroundJobOptions options)
- void AddJob(System.Type jobType, Action<Nerosoft.Euonia.Quartz.BackgroundJobOptions> action)
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions Configure(Action<Quartz.IQuartzBuilder> action)
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions SetProperty(System.String name, System.String value)
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions UseDedicatedThreadPool(System.Int32 maxConcurrency, Action<Quartz.ThreadPoolOptions> configure = null)
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions UseDedicatedThreadPool(Action<Quartz.ThreadPoolOptions> configure = null)
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions UseDefaultThreadPool(System.Int32 maxConcurrency, Action<Quartz.ThreadPoolOptions> configure = null)
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions UseDefaultThreadPool(Action<Quartz.ThreadPoolOptions> configure = null)
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions UseInMemoryStore(Action<Quartz.InMemoryJobStoreOptions> configure = null)
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions UsePersistentStore(Action<Quartz.IPersistentStoreBuilder> configure)
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions UsePersistentStore<T>(Action<Quartz.IPersistentStoreBuilder> configure)
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions UseSimpleTypeLoader()
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions UseThreadPool<T>(Action<Quartz.ThreadPoolOptions> configure = null)
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions UseTypeLoader<T>()
- Nerosoft.Euonia.Quartz.BackgroundBuildOptions UseZeroSizeThreadPool(Action<Quartz.ThreadPoolOptions> configure = null)

### Nerosoft.Euonia.Quartz.BackgroundJobAttribute

**类型**：class
基类 System.Attribute

- new Nerosoft.Euonia.Quartz.BackgroundJobAttribute(System.String name)
- System.String Description { get; set; }
- System.String Group { get; set; }
- System.String Name { get; }
- Nullable<System.Boolean> RequestRecovery { get; set; }
- Nullable<System.Boolean> StoreDurably { get; set; }

### Nerosoft.Euonia.Quartz.BackgroundJobOptions

**类型**：class

- new Nerosoft.Euonia.Quartz.BackgroundJobOptions()
- System.String Group { get; set; }
- Action<IJobConfigurator<Quartz.IJob>> JobConfigure { get; set; }
- System.String Name { get; set; }
- List<Action<ITriggerConfigurator<Quartz.IJob>>> TriggerConfigure { get; }

### Nerosoft.Euonia.Quartz.BackgroundJobScheduleAttribute

**类型**：class
基类 System.Attribute

- System.Int32 Delay { get; set; }
- System.String Description { get; set; }
- System.String Identity { get; }
- Nullable<System.Int32> MisfirePolicy { get; set; }
- System.Int32 Priority { get; set; }
- Quartz.IScheduleBuilder Configure()

### Nerosoft.Euonia.Quartz.CalendarIntervalScheduleAttribute

**类型**：class
基类 Nerosoft.Euonia.Quartz.BackgroundJobScheduleAttribute

- new Nerosoft.Euonia.Quartz.CalendarIntervalScheduleAttribute(System.String identity, System.Int32 interval, Quartz.IntervalUnit unit)
- System.Int32 Interval { get; }
- Nullable<System.Boolean> PreserveHourOfDayAcrossDaylightSavings { get; set; }
- Nullable<System.Boolean> SkipDayIfHourDoesNotExist { get; set; }
- Quartz.IntervalUnit Unit { get; }
- Quartz.IScheduleBuilder Configure()

### Nerosoft.Euonia.Quartz.CronExpressionScheduleAttribute

**类型**：class
基类 Nerosoft.Euonia.Quartz.BackgroundJobScheduleAttribute

- new Nerosoft.Euonia.Quartz.CronExpressionScheduleAttribute(System.String identity, System.String cronExpression)
- new Nerosoft.Euonia.Quartz.CronExpressionScheduleAttribute(System.String identity, System.Int32 hour, System.Int32 minute, System.DayOfWeek[] daysOfWeek)
- new Nerosoft.Euonia.Quartz.CronExpressionScheduleAttribute(System.String identity, System.Int32 hour, System.Int32 minute, System.Int32 dayOfMonth)
- System.String CronExpression { get; }
- System.String TimeZoneName { get; set; }
- Quartz.IScheduleBuilder Configure()

### Nerosoft.Euonia.Quartz.DailyTimeIntervalScheduleAttribute

**类型**：class
基类 Nerosoft.Euonia.Quartz.BackgroundJobScheduleAttribute

- new Nerosoft.Euonia.Quartz.DailyTimeIntervalScheduleAttribute(System.String identity, System.Int32 interval, Quartz.IntervalUnit unit)
- System.Int32 Interval { get; }
- Nullable<System.Int32> RepeatCount { get; set; }
- Quartz.IntervalUnit Unit { get; }
- Quartz.IScheduleBuilder Configure()

### Nerosoft.Euonia.Quartz.DefaultJobFactory

**类型**：class
接口 Quartz.Extensibility.IJobFactory

- new Nerosoft.Euonia.Quartz.DefaultJobFactory(System.IServiceProvider provider)
- ValueTask<Quartz.Extensibility.JobScope> CreateJob(Quartz.Extensibility.TriggerFiredBundle bundle, Quartz.IScheduler scheduler, System.Threading.CancellationToken cancellationToken)
- System.Threading.Tasks.ValueTask ReturnJob(Quartz.Extensibility.JobScope scope, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Quartz.QuartzExtensions

**类型**：static class

- static Quartz.IQuartzBuilder AddJob<TJob>(Quartz.IQuartzBuilder configurator, Nerosoft.Euonia.Quartz.BackgroundJobOptions options = null)
- static Quartz.IQuartzBuilder AddJob(Quartz.IQuartzBuilder configurator, System.Type jobType, Nerosoft.Euonia.Quartz.BackgroundJobOptions options = null)
- static Quartz.IQuartzBuilder AddJobs(Quartz.IQuartzBuilder configurator, System.Reflection.Assembly assembly)
- static ITriggerConfigurator<Quartz.IJob> Configure(ITriggerConfigurator<Quartz.IJob> config, Nerosoft.Euonia.Quartz.BackgroundJobScheduleAttribute attribute, Quartz.JobKey jobKey)
- static ITriggerConfigurator<Quartz.IJob> Configure<TSchedule>(ITriggerConfigurator<Quartz.IJob> config, TSchedule attribute, Quartz.JobKey jobKey)

### Nerosoft.Euonia.Quartz.QuartzTelemetry

**类型**：static class

- static const System.String DurationMetric = euonia.quartz.job.duration
- static const System.String ExecuteActivity = euonia.quartz.execute
- static const System.String FailuresMetric = euonia.quartz.job.failures
- static readonly System.Diagnostics.ActivitySource Source
- static const System.String SourceName = Nerosoft.Euonia.Quartz
- static void Complete(System.Diagnostics.Activity activity, System.Boolean success, System.Double elapsedSeconds)
- static System.Diagnostics.Activity Start(System.String jobKey, System.String jobGroup)

### Nerosoft.Euonia.Quartz.QuartzTelemetryMiddleware

**类型**：class
接口 Quartz.IJobExecutionMiddleware

- new Nerosoft.Euonia.Quartz.QuartzTelemetryMiddleware()
- System.Threading.Tasks.ValueTask Invoke(Quartz.IJobExecutionContext context, Quartz.JobExecutionDelegate next, System.Threading.CancellationToken cancellationToken)

### Nerosoft.Euonia.Quartz.SimpleScheduleAttribute

**类型**：class
基类 Nerosoft.Euonia.Quartz.BackgroundJobScheduleAttribute

- new Nerosoft.Euonia.Quartz.SimpleScheduleAttribute(System.String identity, System.Int32 milliseconds)
- new Nerosoft.Euonia.Quartz.SimpleScheduleAttribute(System.String identity, System.Int32 hour, System.Int32 minute, System.Int32 second)
- System.TimeSpan Interval { get; }
- Nullable<System.Int32> RepeatCount { get; set; }
- Quartz.IScheduleBuilder Configure()
