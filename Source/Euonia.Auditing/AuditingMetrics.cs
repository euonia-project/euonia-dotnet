using System.Diagnostics.Metrics;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计自身的指标：记录数与写入结果，不含租户标签。
/// </summary>
public static class AuditingMetrics
{
	/// <summary>Meter 名称。</summary>
	public const string MeterName = "Nerosoft.Euonia.Auditing";

	/// <summary>记录数指标。</summary>
	public const string RecordsMetric = "euonia.auditing.records";

	/// <summary>写入失败指标。</summary>
	public const string WriteFailuresMetric = "euonia.auditing.write.failures";

	/// <summary>写入耗时指标（秒）。</summary>
	public const string WriteDurationMetric = "euonia.auditing.write.duration";

	private static readonly Meter _meter = new(MeterName);
	private static readonly Counter<long> _records = _meter.CreateCounter<long>(RecordsMetric, "{record}");
	private static readonly Counter<long> _failures = _meter.CreateCounter<long>(WriteFailuresMetric, "{failure}");
	private static readonly Histogram<double> _duration = _meter.CreateHistogram<double>(WriteDurationMetric, "s");

	/// <summary>记录一次写入结果。</summary>
	/// <param name="success">是否成功。</param>
	/// <param name="elapsedSeconds">耗时（秒）。</param>
	public static void RecordWrite(bool success, double elapsedSeconds)
	{
		_records.Add(1);
		_duration.Record(elapsedSeconds);
		if (!success)
		{
			_failures.Add(1);
		}
	}
}
