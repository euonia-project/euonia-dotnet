using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// Osba 的可观测性入口：<see cref="ActivitySource"/>、<see cref="Meter"/> 与日志回退。
/// </summary>
/// <remarks>
/// <para>
/// 外部通过名字订阅即可，不需要引用 Osba 的类型：
/// <c>ActivitySource("Euonia.Osba")</c>、<c>Meter("Euonia.Osba")</c>。
/// </para>
/// <para>
/// 仪表只在有监听者时记录（<see cref="Instrument.Enabled"/>），没有监听时开销接近零；
/// 日志优先使用宿主注册的 <see cref="ILoggerFactory"/>，未注册时回退到 <see cref="Trace"/>。
/// </para>
/// </remarks>
internal static class OsbaDiagnostics
{
	/// <summary>
	/// ActivitySource 名称。
	/// </summary>
	public const string SourceName = "Euonia.Osba";

	/// <summary>
	/// 是否启用指标/追踪；由 <see cref="OsbaOptions.EnableDiagnostics"/> 控制。
	/// </summary>
	private static int _enabled = 1;

	/// <summary>
	/// 获取或设置是否启用指标/追踪。
	/// </summary>
	public static bool Enabled
	{
		get => Volatile.Read(ref _enabled) == 1;
		set => Volatile.Write(ref _enabled, value ? 1 : 0);
	}

	/// <summary>
	/// 分布式追踪来源。
	/// </summary>
	public static readonly ActivitySource ActivitySource = new(SourceName);

	/// <summary>
	/// 指标仪表。
	/// </summary>
	private static readonly Meter Meter = new(SourceName, "1.0.0");

	/// <summary>
	/// 保存耗时（毫秒）。
	/// </summary>
	private static readonly Histogram<double> SaveDuration = Meter.CreateHistogram<double>(
		"osba.save.duration", unit: "ms", description: "Business object save duration.");

	/// <summary>
	/// 授权判定耗时（毫秒）。
	/// </summary>
	private static readonly Histogram<double> AuthorizationDuration = Meter.CreateHistogram<double>(
		"osba.auth.duration", unit: "ms", description: "Object/scope authorization duration.");

	/// <summary>
	/// 规则执行耗时（毫秒）。
	/// </summary>
	private static readonly Histogram<double> RuleDuration = Meter.CreateHistogram<double>(
		"osba.rule.duration", unit: "ms", description: "Rule execution duration.");

	/// <summary>
	/// 规则失败次数。
	/// </summary>
	private static readonly Counter<long> RuleFailures = Meter.CreateCounter<long>(
		"osba.rule.failures", description: "Number of failed rule executions.");

	/// <summary>
	/// 保存失败次数。
	/// </summary>
	private static readonly Counter<long> SaveFailures = Meter.CreateCounter<long>(
		"osba.save.failures", description: "Number of failed save operations.");

	/// <summary>
	/// 开启一次保存活动。
	/// </summary>
	/// <param name="objectType">对象类型名。</param>
	/// <param name="operation">业务操作。</param>
	/// <returns>活动；无监听者时为 <see langword="null"/>。</returns>
	public static Activity StartSave(string objectType, string operation)
	{
		if (!Enabled)
		{
			return null;
		}

		var activity = ActivitySource.StartActivity("osba.save");
		if (activity != null)
		{
			activity.SetTag("osba.object.type", objectType);
			activity.SetTag("osba.operation", operation);
		}

		return activity;
	}

	/// <summary>
	/// 记录一次保存结果。
	/// </summary>
	/// <param name="startTimestamp">起始 <see cref="Stopwatch.GetTimestamp"/>。</param>
	/// <param name="objectType">对象类型名。</param>
	/// <param name="operation">业务操作。</param>
	/// <param name="succeeded">是否成功。</param>
	public static void RecordSave(long startTimestamp, string objectType, string operation, bool succeeded)
	{
		if (!Enabled)
		{
			return;
		}

		if (SaveDuration.Enabled)
		{
			SaveDuration.Record(Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds,
				new KeyValuePair<string, object>("osba.object.type", objectType),
				new KeyValuePair<string, object>("osba.operation", operation),
				new KeyValuePair<string, object>("osba.outcome", succeeded ? "success" : "failure"));
		}

		if (!succeeded && SaveFailures.Enabled)
		{
			SaveFailures.Add(1,
				new KeyValuePair<string, object>("osba.object.type", objectType),
				new KeyValuePair<string, object>("osba.operation", operation));
		}
	}

	/// <summary>
	/// 记录一次授权判定结果。
	/// </summary>
	/// <param name="startTimestamp">起始时间戳。</param>
	/// <param name="kind">判定类型（object / scope）。</param>
	/// <param name="operation">业务操作。</param>
	/// <param name="succeeded">是否通过。</param>
	public static void RecordAuthorization(long startTimestamp, string kind, string operation, bool succeeded)
	{
		if (!Enabled || !AuthorizationDuration.Enabled)
		{
			return;
		}

		AuthorizationDuration.Record(Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds,
			new KeyValuePair<string, object>("osba.auth.kind", kind),
			new KeyValuePair<string, object>("osba.operation", operation),
			new KeyValuePair<string, object>("osba.outcome", succeeded ? "success" : "failure"));
	}

	/// <summary>
	/// 记录一次规则执行结果。
	/// </summary>
	/// <param name="startTimestamp">起始时间戳。</param>
	/// <param name="ruleName">规则名称。</param>
	/// <param name="succeeded">是否通过。</param>
	public static void RecordRule(long startTimestamp, string ruleName, bool succeeded)
	{
		if (!Enabled)
		{
			return;
		}

		if (RuleDuration.Enabled)
		{
			RuleDuration.Record(Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds,
				new KeyValuePair<string, object>("osba.rule.name", ruleName),
				new KeyValuePair<string, object>("osba.outcome", succeeded ? "success" : "failure"));
		}

		if (!succeeded && RuleFailures.Enabled)
		{
			RuleFailures.Add(1, new KeyValuePair<string, object>("osba.rule.name", ruleName));
		}
	}

	/// <summary>
	/// 写入诊断日志：优先使用宿主的 <see cref="ILoggerFactory"/>，否则回退到 <see cref="Trace"/>。
	/// </summary>
	/// <param name="provider">服务提供程序；可为 <see langword="null"/>。</param>
	/// <param name="level">级别。</param>
	/// <param name="message">消息。</param>
	/// <param name="exception">异常；可为 <see langword="null"/>。</param>
	public static void Log(IServiceProvider provider, OsbaDiagnosticLevel level, string message, Exception exception = null)
	{
		if (level == OsbaDiagnosticLevel.None)
		{
			return;
		}

		var loggerFactory = provider?.GetService<ILoggerFactory>();
		if (loggerFactory != null)
		{
			var logger = loggerFactory.CreateLogger(SourceName);
			logger.Log(Map(level), exception, "{Message}", message);
			return;
		}

		Trace.WriteLine($"[{SourceName}] {level}: {message}{(exception == null ? string.Empty : $" ({exception.GetType().Name}: {exception.Message})")}");
	}

	/// <summary>
	/// 把 Osba 诊断级别映射到 <see cref="LogLevel"/>。
	/// </summary>
	private static LogLevel Map(OsbaDiagnosticLevel level)
	{
		return level switch
		{
			OsbaDiagnosticLevel.Trace => LogLevel.Trace,
			OsbaDiagnosticLevel.Debug => LogLevel.Debug,
			OsbaDiagnosticLevel.Information => LogLevel.Information,
			OsbaDiagnosticLevel.Warning => LogLevel.Warning,
			OsbaDiagnosticLevel.Error => LogLevel.Error,
			_ => LogLevel.Warning
		};
	}
}
