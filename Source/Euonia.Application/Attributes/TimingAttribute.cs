namespace Nerosoft.Euonia.Application;

/// <summary>
/// 标记方法（或类，作用于其全部方法）需要记录执行耗时时长。
/// </summary>
/// <remarks>
/// 由 <see cref="TimingInterceptor"/> 处理：用 <see cref="System.Diagnostics.Stopwatch"/> 度量方法执行耗时，
/// 大于等于 <see cref="ThresholdMs"/> 时输出日志（异步方法以任务完成时刻计）。可用于慢调用监控。
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class TimingAttribute : Attribute
{
	/// <summary>
	/// 获取或设置 Information 阈值（毫秒）；耗时大于等于该值时记录 Information 日志，默认 1000 毫秒。
	/// </summary>
	public double ThresholdMs { get; set; } = 1000;

	/// <summary>
	/// 获取或设置 Warning 阈值（毫秒）；耗时大于等于该值时以 Warning 级别记录（优先于 <see cref="ThresholdMs"/>），默认 5000 毫秒。
	/// </summary>
	public double WarningThresholdMs { get; set; } = 5000;
}