using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 默认遥测源工厂：按组件开关创建并缓存源。
/// </summary>
public sealed class TelemetrySourceFactory : ITelemetrySourceFactory
{
	private readonly ConcurrentDictionary<string, TelemetrySource> _sources = new(StringComparer.Ordinal);
	private readonly IOptionsMonitor<TelemetryOptions> _options;
	private readonly ITelemetryRedactor _redactor;
	private readonly IReadOnlyList<ITelemetryEnricher> _enrichers;
	private readonly ITelemetryContextProvider? _contextProvider;
	private readonly TelemetrySourceRegistry? _registry;

	/// <summary>
	/// 初始化 <see cref="TelemetrySourceFactory"/> 的新实例。
	/// </summary>
	/// <param name="options">遥测选项。</param>
	/// <param name="redactor">标签脱敏器。</param>
	/// <param name="enrichers">标签补充器集合。</param>
	/// <param name="contextProvider">上下文提供程序。</param>
	/// <param name="registry">源注册表。</param>
	public TelemetrySourceFactory(
		IOptionsMonitor<TelemetryOptions> options,
		ITelemetryRedactor redactor,
		IEnumerable<ITelemetryEnricher> enrichers,
		ITelemetryContextProvider? contextProvider = null,
		TelemetrySourceRegistry? registry = null)
	{
		_options = options;
		_redactor = redactor;
		_enrichers = enrichers.ToArray();
		_contextProvider = contextProvider;
		_registry = registry;
	}

	/// <inheritdoc />
	public IReadOnlyCollection<TelemetrySource> Sources => _sources.Values.ToArray();

	/// <inheritdoc />
	public TelemetrySource Create(string sourceName, string? meterName = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);

		return _sources.GetOrAdd(sourceName, name =>
		{
			var options = _options.CurrentValue;
			var component = ResolveComponent(options, name);

			var source = new TelemetrySource(
				name,
				meterName,
				options.Enabled && options.TracesEnabled && (component?.Enabled ?? true) && (component?.TracesEnabled ?? true),
				options.Enabled && options.MetricsEnabled && (component?.Enabled ?? true) && (component?.MetricsEnabled ?? true),
				_redactor,
				_enrichers,
				_contextProvider);

			_registry?.Register(source);
			return source;
		});
	}

	private static TelemetryComponentOptions? ResolveComponent(TelemetryOptions options, string sourceName)
	{
		if (options.Components.TryGetValue(sourceName, out var component))
		{
			return component;
		}

		var shortName = sourceName.StartsWith("Nerosoft.Euonia.", StringComparison.Ordinal)
			                ? sourceName["Nerosoft.Euonia.".Length..]
			                : sourceName.StartsWith("Euonia.", StringComparison.Ordinal)
				                ? sourceName["Euonia.".Length..]
				                : sourceName;

		return options.Components.TryGetValue(shortName, out component) ? component : null;
	}
}
