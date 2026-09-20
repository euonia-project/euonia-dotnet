namespace Nerosoft.Euonia.Application;

/// <summary>
/// 标记方法在幂等窗口内对相同指纹的重复调用只执行一次。
/// </summary>
/// <remarks>
/// 由 <see cref="IdempotentInterceptor"/> 处理：调用前先按指纹加锁串行检查，窗口内相同指纹的后续调用
/// 直接返回首次执行的结果（有返回值的方法）或直接跳过（<c>void</c>/<see cref="Task"/> 方法），
/// 防止重复提交导致副作用叠加。
/// </remarks>
[AttributeUsage(AttributeTargets.Method)]
public sealed class IdempotentAttribute : Attribute
{
	/// <summary>
	/// 获取或设置幂等键模板。
	/// </summary>
	/// <remarks>
	/// 支持占位符 {service}（服务类型全名）、{method}（方法名）与按序参数占位符（0、1...）。
	/// 未设置且请求上下文不存在 <c>Idempotency-Key</c> 请求头时，默认以 <c>{service}.{method}:args</c> 为键。
	/// </remarks>
	public string Key { get; set; }

	/// <summary>
	/// 获取或设置幂等窗口秒数；窗口内相同键的重复调用去重，默认 60 秒。
	/// </summary>
	/// <remarks>
	/// 该值同时用于并发重复调用的锁等待时长（毫秒换算）。
	/// </remarks>
	public double TimeoutSeconds { get; set; } = 60;

	/// <summary>
	/// 获取或设置是否优先采用请求头 <c>Idempotency-Key</c> 作为指纹（默认 true）。
	/// </summary>
	/// <remarks>
	/// 为 true 且请求上下文带 <c>Idempotency-Key</c> 头时，指纹仅与「方法名 + 请求键」相关而与实参无关。
	/// </remarks>
	public bool UseRequestKey { get; set; } = true;
}