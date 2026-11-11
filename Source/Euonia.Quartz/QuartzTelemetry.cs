using System.Diagnostics;
using System.Diagnostics.Metrics;
using Quartz;

namespace Nerosoft.Euonia.Quartz;

/// <summary>
/// Quartz 组件的遥测：作业执行 Span 与指标。
/// </summary>
public static class QuartzTelemetry
{
	/// <summary>ActivitySource / Meter 名称。</summary>
	public const string SourceName = "Nerosoft.Euonia.Quartz";

	/// <summary>作业执行活动。</summary>
	public const string ExecuteActivity = "euonia.quartz.execute";

	/// <summary>作业耗时指标（秒）。</summary>
	public const string DurationMetric = "euonia.quartz.job.duration";

	/// <summary>作业失败指标。</summary>
	public const string FailuresMetric = "euonia.quartz.job.failures";

	/// <summary>底层 ActivitySource。</summary>
	public static readonly ActivitySource Source = new(SourceName);

	private static readonly Meter _meter = new(SourceName);
	private static readonly Histogram<double> _duration = _meter.CreateHistogram<double>(DurationMetric, "s");
	private static readonly Counter<long> _failures = _meter.CreateCounter<long>(FailuresMetric, "{failure}");

	/// <summary>启动作业执行活动。</summary>
	/// <param name="jobKey">作业键。</param>
	/// <param name="jobGroup">作业分组。</param>
	/// <returns>活动；未监听时为 <c>null</c>。</returns>
	public static Activity? Start(string jobKey, string? jobGroup)
	{
		return Source.StartActivity(
			ExecuteActivity,
			ActivityKind.Internal,
			Activity.Current?.Context ?? default,
			[
				new KeyValuePair<string, object?>("job.system", "quartz"),
				new KeyValuePair<string, object?>("job.key", jobKey),
				new KeyValuePair<string, object?>("job.group", jobGroup),
			]);
	}

	/// <summary>记录作业执行结果。</summary>
	/// <param name="activity">活动。</param>
	/// <param name="success">是否成功。</param>
	/// <param name="elapsedSeconds">耗时（秒）。</param>
	public static void Complete(Activity? activity, bool success, double elapsedSeconds)
	{
		_duration.Record(elapsedSeconds);
		if (!success)
		{
			_failures.Add(1);
		}

		if (activity == null)
		{
			return;
		}

		activity.SetTag("euonia.outcome", success ? "success" : "failure");
		if (!success)
		{
			activity.SetStatus(ActivityStatusCode.Error);
		}

		activity.Dispose();
	}
}

/// <summary>
/// 作业执行中间件：为每次 Quartz 作业执行产生 Span 并记录耗时。
/// </summary>
public sealed class QuartzTelemetryMiddleware : IJobExecutionMiddleware
{
	/// <inheritdoc />
	public async ValueTask Invoke(IJobExecutionContext context, JobExecutionDelegate next, CancellationToken cancellationToken)
	{
		var key = context.JobDetail.Key;
		var activity = QuartzTelemetry.Start(key.Name, key.Group);
		var stopwatch = Stopwatch.StartNew();
		var success = false;

		try
		{
			await next(context, cancellationToken).ConfigureAwait(false);
			success = true;
		}
		finally
		{
			stopwatch.Stop();
			QuartzTelemetry.Complete(activity, success, stopwatch.Elapsed.TotalSeconds);
		}
	}
}
