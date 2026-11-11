using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Nerosoft.Euonia.Caching;

/// <summary>
/// 缓存组件的遥测入口：ActivitySource 与 Meter 名称、缓存操作 Span 与指标。
/// </summary>
/// <remarks>
/// 标签只使用低基数值（操作、提供程序、结果），不记录键或值；无监听者时不产生开销。
/// </remarks>
public static class CacheTelemetry
{
	/// <summary>
	/// ActivitySource / Meter 名称。
	/// </summary>
	public const string SourceName = "Nerosoft.Euonia.Caching";

	/// <summary>
	/// 缓存读取活动。
	/// </summary>
	public const string GetActivity = "euonia.cache.get";

	/// <summary>
	/// 缓存写入活动。
	/// </summary>
	public const string SetActivity = "euonia.cache.set";

	/// <summary>
	/// 缓存移除活动。
	/// </summary>
	public const string RemoveActivity = "euonia.cache.remove";

	/// <summary>
	/// 请求计数指标。
	/// </summary>
	public const string RequestsMetric = "euonia.cache.requests";

	/// <summary>
	/// 耗时指标（秒）。
	/// </summary>
	public const string DurationMetric = "euonia.cache.duration";

	/// <summary>
	/// 底层 ActivitySource。
	/// </summary>
	public static readonly ActivitySource Source = new(SourceName);

	private static readonly Meter _meter = new(SourceName);
	private static readonly Counter<long> _requests = _meter.CreateCounter<long>(RequestsMetric, "{request}");
	private static readonly Histogram<double> _duration = _meter.CreateHistogram<double>(DurationMetric, "s");

	/// <summary>
	/// 启动缓存操作活动。
	/// </summary>
	/// <param name="name">活动名称。</param>
	/// <param name="operation">操作类型。</param>
	/// <param name="system">缓存提供程序。</param>
	/// <returns>活动；未监听时为 <c>null</c>。</returns>
	public static Activity? Start(string name, string operation, string system)
	{
		return Source.StartActivity(
			name,
			ActivityKind.Internal,
			Activity.Current?.Context ?? default,
			[
				new KeyValuePair<string, object?>("cache.operation", operation),
				new KeyValuePair<string, object?>("cache.system", system),
			]);
	}

	/// <summary>
	/// 记录一次缓存请求。
	/// </summary>
	/// <param name="operation">操作类型。</param>
	/// <param name="result">结果（hit / miss / ok）。</param>
	/// <param name="elapsedSeconds">耗时（秒）。</param>
	public static void Record(string operation, string result, double elapsedSeconds)
	{
		var tags = new TagList
		{
			{ "cache.operation", operation },
			{ "cache.result", result },
		};

		_requests.Add(1, tags);
		_duration.Record(elapsedSeconds, tags);
	}

	/// <summary>
	/// 结束活动并记录结果。
	/// </summary>
	/// <param name="activity">活动。</param>
	/// <param name="operation">操作类型。</param>
	/// <param name="success">是否成功。</param>
	/// <param name="stopwatch">计时器。</param>
	public static void Complete(Activity? activity, string operation, bool success, Stopwatch stopwatch)
	{
		stopwatch.Stop();
		Record(operation, success ? "ok" : "failure", stopwatch.Elapsed.TotalSeconds);

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
