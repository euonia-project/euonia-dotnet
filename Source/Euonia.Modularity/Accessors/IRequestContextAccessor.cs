namespace Nerosoft.Euonia.Modularity;

/// <summary>
/// 定义访问当前 <see cref="RequestContext"/> 的契约接口。
/// </summary>
/// <remarks>
/// <para>
/// 该接口作为底层上下文存储机制的抽象层（如 <see cref="DefaultRequestContextAccessor"/> 或
/// <see cref="DelegateRequestContextAccessor"/>）。
/// </para>
/// <para>
/// 实现类型以 <see cref="ISingletonDependency"/> 生命周期注册，因此访问器实例本身在整个应用生命周期内共享。
/// 线程安全与请求级隔离由底层存储（通常为 <c>AsyncLocal&lt;T&gt;</c>）保障，
/// 而非通过每次请求创建新的访问器实例来实现。
/// </para>
/// <para>
/// 典型用法：中间件或拦截器在请求开始时设置上下文，应用/领域代码通过 getter 读取上下文。
/// </para>
/// </remarks>
/// <seealso cref="DefaultRequestContextAccessor"/>
/// <seealso cref="DelegateRequestContextAccessor"/>
/// <seealso cref="RequestContext"/>
public interface IRequestContextAccessor : ISingletonDependency
{
	/// <summary>
	/// 获取当前异步执行流绑定的 <see cref="RequestContext"/>。
	/// </summary>
	/// <value>
	/// 当前异步执行上下文中的 <see cref="RequestContext"/> 实例；
	/// 如果尚未设置上下文（例如在 HTTP 请求作用域之外），则返回 <c>null</c>。
	/// </value>
	RequestContext Context { get; }
}
