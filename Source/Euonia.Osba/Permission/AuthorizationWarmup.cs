using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 异步授权路径的<b>预热</b>：在第一次同步判定之前把授权数据解析出来。
/// </summary>
/// <remarks>
/// <para>
/// 同步判定要读的授权数据（权限码集合、行级授予）在引擎里是<b>按请求异步解析并缓存</b>的：
/// 冷缓存首次读取会走 <c>AsyncContext.Run</c>——它起一个私有 <c>SynchronizationContext</c> 并
/// <b>阻塞当前线程</b>直到宿主的解析器（通常做 I/O）完成。
/// </para>
/// <para>
/// 授权判定本身是同步契约（<c>EnsureAuthorized</c> / <c>CanPerformOperation</c> 是可被派生类重写的钩子），
/// 因此 <c>BusinessObjectFactory</c> 的每个 <c>*Async</c> 入口若直接调同步判定，整条链路会退化成
/// sync-over-async，负载下表现为线程池饥饿。
/// </para>
/// <para>
/// 做法：<b>先</b> <c>await</c> 预热，<b>再</b>进入同步判定——此时读缓存命中暖路径直接返回快照，不再阻塞。
/// 预热入口刻意放在 <c>Euonia.Core</c> 的契约上
/// （<see cref="IPermissionChecker.EnsureResolvedAsync"/> / <see cref="IObjectScopeAuthorizer.EnsureResolvedAsync"/>），
/// 因为 <c>Euonia.Osba</c> 按设计<b>不引用</b> <c>Euonia.Security</c>：宿主框架只认契约，
/// 由实现方决定「预热」具体要做什么（默认是空操作，不预热的实现无需改动）。
/// </para>
/// <para>
/// <b>同步入口（<c>Create</c> / <c>Fetch</c> 等同步重载）刻意不预热</b>：它们本就运行在同步契约上，
/// 阻塞是其既定语义；预热只会带来一次不必要的异步调度。
/// </para>
/// </remarks>
internal static class AuthorizationWarmup
{
	/// <summary>
	/// 预热操作权限判定所需的授权数据。
	/// </summary>
	/// <param name="checker">权限判定实现；为 <see langword="null"/> 时无操作返回。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>预热完成的任务。</returns>
	internal static ValueTask WarmAsync(IPermissionChecker checker, CancellationToken cancellationToken)
	{
		return checker == null ? ValueTask.CompletedTask : checker.EnsureResolvedAsync(cancellationToken);
	}

	/// <summary>
	/// 预热行级数据权限判定所需的授权数据。
	/// </summary>
	/// <param name="authorizer">行级判定实现；为 <see langword="null"/> 时无操作返回。</param>
	/// <param name="scope">对象所属作用域的服务提供程序（与判定时用的必须是同一个）。</param>
	/// <param name="cancellationToken">用于取消操作的令牌。</param>
	/// <returns>预热完成的任务。</returns>
	internal static ValueTask WarmAsync(IObjectScopeAuthorizer authorizer, IServiceProvider scope, CancellationToken cancellationToken)
	{
		return authorizer == null ? ValueTask.CompletedTask : authorizer.EnsureResolvedAsync(scope, cancellationToken);
	}
}
