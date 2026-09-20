namespace Nerosoft.Euonia.Application;

/// <summary>
/// 标记方法（或类，作用于其全部方法）需要记录执行耗时时长。
/// </summary>
/// <remarks>
/// 由 <see cref="TimingInterceptor"/> 处理：用 <see cref="System.Diagnostics.Stopwatch"/> 度量方法执行耗时，
/// 超过 <see cref="ThresholdMs"/> 时输出 Information 日志（异步方法以任务完成时刻计）。可用于慢调用监控。
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class TimingAttribute : Attribute
{
	/// <summary>
	/// 获取或设置告警阈值（毫秒）；耗时大于等于该值时记录日志，默认 1000 毫秒。
	/// </summary>
	public double ThresholdMs { get; set; } = 1000;
}