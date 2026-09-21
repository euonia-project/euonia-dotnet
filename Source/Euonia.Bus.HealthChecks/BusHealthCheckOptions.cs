namespace Nerosoft.Euonia.Bus.HealthChecks;

/// <summary>
/// 消息总线健康检查的配置选项。
/// </summary>
public class BusHealthCheckOptions
{
	/// <summary>
	/// 获取或设置允许的未投递死信条数上限。
	/// </summary>
	/// <remarks>
	/// 超过该值即判定为 <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy"/>。
	/// 默认 <c>0</c>，即只要出现死信就不健康。
	/// </remarks>
	public int MaxDeadLetters { get; set; }

	/// <summary>
	/// 获取或设置允许的未投递发件箱失败记录数上限。
	/// </summary>
	/// <remarks>
	/// 超过该值即判定为 <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy"/>。
	/// 默认 <c>-1</c>，表示不检查发件箱积压。
	/// </remarks>
	public int MaxOutboxFailed { get; set; } = -1;

	/// <summary>
	/// 获取或设置允许的未处理收件箱失败记录数上限。
	/// </summary>
	/// <remarks>
	/// 超过该值即判定为 <see cref="Microsoft.Extensions.Diagnostics.HealthChecks.HealthStatus.Unhealthy"/>。
	/// 默认 <c>-1</c>，表示不检查收件箱积压。
	/// </remarks>
	public int MaxInboxFailed { get; set; } = -1;

	/// <summary>
	/// 获取或设置一个值，指示在未注册任何传输器时是否判定为不健康。
	/// </summary>
	/// <remarks>
	/// 默认 <c>false</c>：某些宿主只消费不发送，未注册传输器属正常情况。
	/// </remarks>
	public bool RequireTransporter { get; set; }
}
