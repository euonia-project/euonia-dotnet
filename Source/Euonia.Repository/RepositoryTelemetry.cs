using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Nerosoft.Euonia.Repository;

/// <summary>
/// 仓储组件的遥测入口：执行 Span 与耗时指标。
/// </summary>
/// <remarks>
/// 标签只使用操作与实体名，不记录 SQL、连接串或参数；无监听者时不产生开销。
/// </remarks>
public static class RepositoryTelemetry
{
	/// <summary>
	/// ActivitySource / Meter 名称。
	/// </summary>
	public const string SourceName = "Nerosoft.Euonia.Repository";

	/// <summary>
	/// 仓储执行活动。
	/// </summary>
	public const string ExecuteActivity = "euonia.repository.execute";

	/// <summary>
	/// 执行耗时指标（秒）。
	/// </summary>
	public const string DurationMetric = "euonia.repository.duration";

	/// <summary>
	/// 底层 ActivitySource。
	/// </summary>
	public static readonly ActivitySource Source = new(SourceName);

	private static readonly Meter _meter = new(SourceName);
	private static readonly Histogram<double> _duration = _meter.CreateHistogram<double>(DurationMetric, "s");

	/// <summary>
	/// 启动仓储执行活动。
	/// </summary>
	/// <param name="operation">操作类型。</param>
	/// <param name="entityName">实体名称。</param>
	/// <returns>活动；未监听时为 <c>null</c>。</returns>
	public static Activity? Start(string operation, string entityName)
	{
		return Source.StartActivity(
			ExecuteActivity,
			ActivityKind.Internal,
			Activity.Current?.Context ?? default,
			[
				new KeyValuePair<string, object?>("db.operation.name", operation),
				new KeyValuePair<string, object?>("db.collection.name", entityName),
			]);
	}

	/// <summary>
	/// 结束仓储执行活动并记录耗时。
	/// </summary>
	/// <param name="activity">活动。</param>
	/// <param name="operation">操作类型。</param>
	/// <param name="success">是否成功。</param>
	/// <param name="stopwatch">计时器。</param>
	public static void Complete(Activity? activity, string operation, bool success, Stopwatch stopwatch)
	{
		stopwatch.Stop();
		_duration.Record(stopwatch.Elapsed.TotalSeconds, new TagList
		{
			{ "db.operation.name", operation },
			{ "euonia.outcome", success ? "success" : "failure" },
		});

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
