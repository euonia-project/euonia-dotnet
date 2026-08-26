namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// <see cref="IRequestContextAccessor"/> 的默认底层存储实现，
/// 使用 <see cref="AsyncLocal{T}"/> 保存当前请求的 <see cref="RequestContext"/>。
/// </summary>
/// <remarks>
/// <para>
/// 本类并非直接实现 <see cref="IRequestContextAccessor"/> 接口，
/// 而是由 <see cref="RequestContextAccessor"/> 组合并委托调用。
/// </para>
/// <para>
/// 内部采用 <see cref="AsyncLocal{T}"/> 存储请求上下文，
/// 确保上下文数据在异步调用链（<c>async/await</c>）中自动传播，
/// 同时隔离不同请求/线程之间的数据，避免并发冲突。
/// </para>
/// <para>
/// 由于 <see cref="AsyncLocal{T}"/> 的特性，本类型可安全地注册为任意生命周期
/// （Singleton、Scoped 或 Transient）。在单例场景下，每个异步执行流
/// 仍然拥有各自独立的 <see cref="RequestContext"/> 值。
/// </para>
/// </remarks>
/// <seealso cref="IRequestContextAccessor"/>
/// <seealso cref="RequestContextAccessor"/>
public class DefaultRequestContextAccessor
{
	/// <summary>
	/// 使用 <see cref="AsyncLocal{T}"/> 存储当前异步执行流中的 <see cref="RequestContext"/> 实例。
	/// </summary>
	/// <remarks>
	/// <see cref="AsyncLocal{T}"/> 保证值沿 <c>async/await</c> 调用链向下流动，
	/// 并在不同异步执行流之间相互隔离。
	/// </remarks>
	private static readonly AsyncLocal<RequestContext> _context = new();

	/// <summary>
	/// 获取或设置当前异步执行流中的 <see cref="RequestContext"/> 实例。
	/// </summary>
	/// <value>
	/// 当前请求上下文；如果当前执行流中尚未设置上下文，则返回 <c>null</c>。
	/// </value>
	/// <remarks>
	/// <para>
	/// 在 HTTP 请求管道中，中间件或过滤器通常在请求开始时设置此属性，
	/// 在请求结束后该值会随执行流的结束而自动清除。
	/// </para>
	/// <para>
	/// 设置为 <c>null</c> 可显式清除当前执行流中的请求上下文。
	/// </para>
	/// </remarks>
	public RequestContext Context
	{
		get => _context.Value;
		set => _context.Value = value;
	}
}