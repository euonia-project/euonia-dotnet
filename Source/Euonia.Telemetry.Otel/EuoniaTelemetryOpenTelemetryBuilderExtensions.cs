using Microsoft.Extensions.DependencyInjection;
using OpenTelemetry.Trace;

namespace Nerosoft.Euonia.Telemetry.Otel;

/// <summary>
/// 兼容以 <see cref="TracerProviderBuilder"/> 为入口的接入方式。
/// </summary>
public static class EuoniaTelemetryOpenTelemetryBuilderExtensions
{
	/// <summary>
	/// 添加 OTLP 导出器；等价于 <c>AddOtlpExporter()</c>，仅用于表达式上下文中保持可读性。
	/// </summary>
	/// <param name="builder">跟踪构建器。</param>
	/// <returns>跟踪构建器。</returns>
	public static TracerProviderBuilder AddEuoniaOtlpExporter(this TracerProviderBuilder builder)
	{
		ArgumentNullException.ThrowIfNull(builder);
		return builder.AddOtlpExporter();
	}
}
