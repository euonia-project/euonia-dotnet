namespace Nerosoft.Euonia.Application;

/// <summary>
/// 定义重试时相邻两次尝试之间的退避策略。
/// </summary>
public enum RetryBackoffMode
{
	/// <summary>
	/// 每次重试间隔固定为 <see cref="RetryAttribute.DelayMs"/> 毫秒。
	/// </summary>
	Fixed = 0,

	/// <summary>
	/// 间隔随重试次数线性递增（第 n 次重试间隔 n × <see cref="RetryAttribute.DelayMs"/>）。
	/// </summary>
	Linear = 1,

	/// <summary>
	/// 间隔随重试次数指数递增（第 n 次重试间隔 2^(n-1) × <see cref="RetryAttribute.DelayMs"/>）。
	/// </summary>
	Exponential = 2,
}