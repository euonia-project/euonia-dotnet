namespace Nerosoft.Euonia.Application;

/// <summary>
/// 标记方法（或类，作用于其全部方法）在抛出可重试异常时按策略重试。
/// </summary>
/// <remarks>
/// 由 <see cref="RetryInterceptor"/> 处理：每次尝试失败且异常类型匹配 <see cref="RetryableExceptions"/>（未指定则任意异常）
/// 且未超过 <see cref="MaxRetries"/> 时，按 <see cref="Backoff"/> 与 <see cref="DelayMs"/> 退避后重新执行。
/// <para>支持同步方法与返回 <see cref="Task"/>/<see cref="Task{TResult}"/>、<see cref="ValueTask"/>/<see cref="ValueTask{TResult}"/>
/// 的异步方法（异步方法以任务失败时刻判定）。</para>
/// </remarks>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
public sealed class RetryAttribute : Attribute
{
	/// <summary>
	/// 获取或设置首次失败后的最大重试次数（不含首次尝试），默认 3。
	/// </summary>
	public int MaxRetries { get; set; } = 3;

	/// <summary>
	/// 获取或设置相邻两次尝试之间的基础退避毫秒数，默认 0（立即重试）。
	/// </summary>
	public int DelayMs { get; set; }

	/// <summary>
	/// 获取或设置退避策略，默认 <see cref="RetryBackoffMode.Fixed"/>。
	/// </summary>
	public RetryBackoffMode Backoff { get; set; } = RetryBackoffMode.Fixed;

	/// <summary>
	/// 获取或设置是否在退避间隔上加入抖动（jitter）；为 true 时实际间隔在 [0, 计算值] 内随机化，默认 false。
	/// </summary>
	/// <remarks>
	/// 用于多节点场景：避免固定/线性/指数退避下各节点在同一时刻同步唤醒造成惊群。
	/// </remarks>
	public bool Jitter { get; set; }

	/// <summary>
	/// 获取或设置应重试的异常类型；为空表示对任意异常重试。
	/// </summary>
	/// <remarks>
	/// 匹配时遍历异常链（含 <see cref="Exception.InnerException"/>），异常链中任意节点可赋值给任一列出的类型即视为可重试。
	/// </remarks>
	public Type[] RetryableExceptions { get; set; }
}