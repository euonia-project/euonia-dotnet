using System.Collections.Concurrent;

namespace Nerosoft.Euonia.Telemetry;

/// <summary>
/// 已创建遥测源的注册表，供 OTel 适配层显式采集。
/// </summary>
public sealed class TelemetrySourceRegistry : IDisposable
{
	private readonly ConcurrentDictionary<string, TelemetrySource> _sources = new(StringComparer.Ordinal);

	/// <summary>
	/// 登记一个遥测源。
	/// </summary>
	/// <param name="source">遥测源。</param>
	public void Register(TelemetrySource source)
	{
		ArgumentNullException.ThrowIfNull(source);
		_sources[$"{source.SourceName}|{source.MeterName}"] = source;
	}

	/// <summary>
	/// 获取全部已登记的遥测源。
	/// </summary>
	/// <returns>遥测源快照。</returns>
	public IReadOnlyCollection<TelemetrySource> GetAll()
	{
		return _sources.Values.ToArray();
	}

	/// <inheritdoc />
	/// <remarks>注册表不持有源的所有权；源由创建它的工厂释放。</remarks>
	public void Dispose()
	{
		_sources.Clear();
	}
}
