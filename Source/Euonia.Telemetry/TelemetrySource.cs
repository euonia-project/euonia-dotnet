using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 组件的遥测入口：集中持有 <see cref="ActivitySource"/> 与 <see cref="Meter"/>，并缓存指标实例。
/// </summary>
/// <remarks>
/// 无监听者或关闭时所有方法空操作；异常一律吞掉，绝不改变业务控制流。
/// 标签在写入前经过 <see cref="ITelemetryRedactor"/>，Activity 创建后执行 <see cref="ITelemetryEnricher"/>。
/// </remarks>
public sealed class TelemetrySource : IDisposable
{
	private readonly ConcurrentDictionary<string, Counter<long>> _counters = new(StringComparer.Ordinal);
	private readonly ConcurrentDictionary<string, Histogram<double>> _histograms = new(StringComparer.Ordinal);
	private readonly ITelemetryRedactor? _redactor;
	private readonly IReadOnlyList<ITelemetryEnricher> _enrichers;
	private readonly ITelemetryContextProvider? _contextProvider;
	private static readonly Meter FallbackMeter = new("Euonia.Telemetry.Disposed");
	private static readonly Counter<long> FallbackCounter = FallbackMeter.CreateCounter<long>("noop");
	private static readonly Histogram<double> FallbackHistogram = FallbackMeter.CreateHistogram<double>("noop");

	private int _tracesEnabled;
	private int _metricsEnabled;
	private int _disposed;

	/// <summary>
	/// 初始化 <see cref="TelemetrySource"/> 的新实例。
	/// </summary>
	/// <param name="sourceName">ActivitySource 名称，同时作为默认 Meter 名称。</param>
	/// <param name="meterName">Meter 名称；默认与 <paramref name="sourceName"/> 相同。</param>
	/// <param name="tracesEnabled">是否启用跟踪。</param>
	/// <param name="metricsEnabled">是否启用指标。</param>
	/// <param name="redactor">标签脱敏器。</param>
	/// <param name="enrichers">标签补充器集合。</param>
	/// <param name="contextProvider">遥测上下文提供程序。</param>
	public TelemetrySource(
		string sourceName,
		string? meterName = null,
		bool tracesEnabled = true,
		bool metricsEnabled = true,
		ITelemetryRedactor? redactor = null,
		IReadOnlyList<ITelemetryEnricher>? enrichers = null,
		ITelemetryContextProvider? contextProvider = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

		SourceName = sourceName;
		MeterName = meterName ?? sourceName;
		_tracesEnabled = tracesEnabled ? 1 : 0;
		_metricsEnabled = metricsEnabled ? 1 : 0;
		_redactor = redactor;
		_enrichers = enrichers ?? [];
		_contextProvider = contextProvider;

		ActivitySource = new ActivitySource(SourceName);
		Meter = new Meter(MeterName);
	}

	/// <summary>
	/// 获取 ActivitySource 名称。
	/// </summary>
	public string SourceName { get; }

	/// <summary>
	/// 获取 Meter 名称。
	/// </summary>
	public string MeterName { get; }

	/// <summary>
	/// 获取底层 ActivitySource。
	/// </summary>
	public ActivitySource ActivitySource { get; }

	/// <summary>
	/// 获取底层 Meter。
	/// </summary>
	public Meter Meter { get; }

	/// <summary>
	/// 获取一个值，指示跟踪是否可用（已启用且存在监听者）。
	/// </summary>
	public bool IsTraceEnabled => Volatile.Read(ref _tracesEnabled) == 1 && ActivitySource.HasListeners();

	/// <summary>
	/// 获取一个值，指示指标是否可用。
	/// </summary>
	public bool IsMetricEnabled => Volatile.Read(ref _metricsEnabled) == 1;

	/// <summary>
	/// 运行时更新开关；由工厂在配置变更时调用。
	/// </summary>
	/// <param name="tracesEnabled">是否启用跟踪。</param>
	/// <param name="metricsEnabled">是否启用指标。</param>
	internal void Update(bool tracesEnabled, bool metricsEnabled)
	{
		Volatile.Write(ref _tracesEnabled, tracesEnabled ? 1 : 0);
		Volatile.Write(ref _metricsEnabled, metricsEnabled ? 1 : 0);
	}

	/// <summary>
	/// 启动活动；不可用时返回空实例。
	/// </summary>
	/// <param name="name">活动名称。</param>
	/// <param name="kind">活动类型。</param>
	/// <param name="parent">父上下文。</param>
	/// <param name="links">关联上下文。</param>
	/// <param name="tags">初始标签。</param>
	/// <returns>活动句柄。</returns>
	public TelemetryActivity StartActivity(
		string name,
		ActivityKind kind = ActivityKind.Internal,
		ActivityContext? parent = null,
		IEnumerable<ActivityLink>? links = null,
		IEnumerable<KeyValuePair<string, object?>>? tags = null)
	{
		if (!IsTraceEnabled)
		{
			return default;
		}

		var previous = Activity.Current;
		Activity? activity = null;

		try
		{
			var parentContext = parent ?? previous?.Context ?? default;
			activity = ActivitySource.StartActivity(name, kind, parentContext, ApplyRedaction(tags), links);

			if (activity == null)
			{
				return default;
			}

			var context = _contextProvider?.GetContext();
			foreach (var enricher in _enrichers)
			{
				enricher.Enrich(activity, context);
			}

			return new TelemetryActivity(activity, _redactor);
		}
		catch (Exception)
		{
			// 富化失败：必须结束已启动的活动并恢复 Activity.Current，否则 span 永不结束且后续 TraceId 错位。
			activity?.Stop();
			Activity.Current = previous;
			return default;
		}
	}

	/// <summary>
	/// 获取或创建计数器。
	/// </summary>
	/// <param name="name">指标名称。</param>
	/// <param name="unit">单位。</param>
	/// <param name="description">描述。</param>
	/// <returns>计数器。</returns>
	public Counter<long> Counter(string name, string? unit = null, string? description = null)
	{
		if (Volatile.Read(ref _disposed) == 1 || Volatile.Read(ref _metricsEnabled) == 0)
		{
			return FallbackCounter;
		}

		return _counters.GetOrAdd(name, key => Meter.CreateCounter<long>(key, unit, description));
	}

	/// <summary>
	/// 获取或创建直方图。
	/// </summary>
	/// <param name="name">指标名称。</param>
	/// <param name="unit">单位。</param>
	/// <param name="description">描述。</param>
	/// <returns>直方图。</returns>
	public Histogram<double> Histogram(string name, string? unit = null, string? description = null)
	{
		if (Volatile.Read(ref _disposed) == 1 || Volatile.Read(ref _metricsEnabled) == 0)
		{
			return FallbackHistogram;
		}

		return _histograms.GetOrAdd(name, key => Meter.CreateHistogram<double>(key, unit, description));
	}

	private KeyValuePair<string, object?>[]? ApplyRedaction(IEnumerable<KeyValuePair<string, object?>>? tags)
	{
		if (tags == null)
		{
			return null;
		}

		return tags.Select(tag => new KeyValuePair<string, object?>(
			             tag.Key,
			             _redactor?.Redact(tag.Key, tag.Value) ?? tag.Value))
		           .ToArray();
	}

	/// <inheritdoc />
	public void Dispose()
	{
		if (Interlocked.Exchange(ref _disposed, 1) == 1)
		{
			return;
		}

		ActivitySource.Dispose();
		Meter.Dispose();
	}
}
