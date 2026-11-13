using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Nerosoft.Euonia.Quartz;
using Nerosoft.Euonia.Telemetry;
using Nerosoft.Euonia.Tenancy;
using Quartz;

namespace Nerosoft.Euonia.Hosting.Tests;

/// <summary>
/// Quartz 作业的租户作用域与执行 Span。
/// </summary>
[Collection("Quartz")]
public class QuartzTenantJobTests
{
	/// <summary>
	/// 作业数据里的 tenantId 在作业执行期间成为当前租户，并产生执行 Span。
	/// </summary>
	[Fact]
	public async Task Job_ShouldRunInsideTenantScope()
	{
		ProbeJob.Reset();
		var spans = new List<Activity>();
		using var listener = new ActivityListener
		{
			ShouldListenTo = source => source.Name == "Nerosoft.Euonia.Quartz",
			Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded,
			ActivityStopped = activity =>
			{
				if (activity.OperationName == QuartzTelemetry.ExecuteActivity)
				{
					lock (spans)
					{
						spans.Add(activity);
					}
				}
			},
		};
		ActivitySource.AddActivityListener(listener);

		var host = new HostBuilder().ConfigureServices(services =>
		{
			services.AddLogging();
			services.AddTenancy(options => options.Enabled = true);
			services.AddInMemoryTenantStore([new TenantInfo { Id = "t1", Name = "T1", Status = TenantStatus.Active }]);
			services.AddQuartz((Action<BackgroundBuildOptions>?)null);
			services.AddTransient<ProbeJob>();
		}).Build();

		await host.StartAsync(TestContext.Current.CancellationToken);

		var scheduler = await host.Services.GetRequiredService<ISchedulerFactory>()
		                        .GetScheduler(TestContext.Current.CancellationToken);
		var job = JobBuilder.Create<ProbeJob>()
		                    .WithIdentity("probe")
		                    .UsingJobData("tenantId", "t1")
		                    .Build();
		var trigger = TriggerBuilder.Create()
		                            .ForJob(job)
		                            .WithIdentity("probe-trigger")
		                            .StartNow()
		                            .Build();
		await scheduler.ScheduleJob(job, trigger);

		await ProbeJob.Completion.Task.WaitAsync(TimeSpan.FromSeconds(15), TestContext.Current.CancellationToken);

		await host.StopAsync(TestContext.Current.CancellationToken);
		host.Dispose();

		Assert.Equal("t1", Assert.Single(ProbeJob.ObservedTenants));

		Activity activity;
		lock (spans)
		{
			activity = Assert.Single(spans);
		}

		Assert.Equal("probe", activity.GetTagItem("job.key"));
	}

	/// <summary>
	/// 记录作业看到的租户。
	/// </summary>
	public sealed class ProbeJob : IJob
	{
		/// <summary>作业完成信号。</summary>
		public static TaskCompletionSource Completion { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		/// <summary>观察到的租户。</summary>
		public static ConcurrentBag<string?> ObservedTenants { get; } = [];

		private readonly ITenantAccessor _accessor;

		/// <summary>
		/// 初始化作业。
		/// </summary>
		/// <param name="accessor">租户访问器。</param>
		public ProbeJob(ITenantAccessor accessor)
		{
			_accessor = accessor;
		}

		/// <summary>
		/// 重置静态状态。
		/// </summary>
		public static void Reset()
		{
			Completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			while (ObservedTenants.TryTake(out _))
			{
			}
		}

		/// <inheritdoc />
		public ValueTask Execute(IJobExecutionContext context, CancellationToken cancellationToken)
		{
			ObservedTenants.Add(_accessor.TenantId);
			Completion.TrySetResult();
			return ValueTask.CompletedTask;
		}
	}
}
