using Microsoft.Extensions.DependencyInjection;
using Nerosoft.Euonia.Security;
using Nerosoft.Euonia.Threading;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 授权数据的<b>预热</b>：在同步判定之前把它解析出来。
/// </summary>
/// <remarks>
/// <para>
/// 判定要读的授权数据（权限码集合、行级授予）由宿主的解析器<b>异步</b>解析，在引擎里按请求缓存。
/// 引擎的同步读<b>只读已解析的快照</b>，冷缓存时直接报错、绝不隐式等待——把一次 I/O 等待藏在判定路径里，
/// 负载下就是线程池饥饿。
/// </para>
/// <para>
/// 于是「什么时候可以等」由<b>宿主</b>决定，这正是本类存在的原因：
/// </para>
/// <list type="bullet">
/// <item><description><b>异步入口</b>（<c>BusinessObjectFactory</c> 的 <c>*Async</c>）<c>await</c> 预热，
/// 之后整条链路走暖路径，全程不阻塞。</description></item>
/// <item><description><b>同步入口</b>（<c>Create</c> / <c>Fetch</c> 等同步重载）本就运行在同步契约上，
/// 阻塞是其既定语义——由本类在入口处<b>显式</b>做掉这一次等待，判定本身仍是同步的。</description></item>
/// </list>
/// <para>
/// 预热入口刻意放在 Core 的契约上（<see cref="IPermissionChecker.EnsureResolvedAsync"/> /
/// <see cref="IObjectScopeAuthorizer.EnsureResolvedAsync"/>），因为 <c>Euonia.Osba</c> 按设计
/// <b>不引用</b> <c>Euonia.Security</c>：宿主框架只认契约，由实现方决定「预热」具体要做什么。
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
		return checker == null ? default : checker.EnsureResolvedAsync(cancellationToken);
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
		return authorizer == null ? default : authorizer.EnsureResolvedAsync(scope, cancellationToken);
	}

	/// <summary>
	/// 同步预热操作权限判定所需的授权数据（为同步入口阻塞一次）。
	/// </summary>
	/// <param name="checker">权限判定实现；为 <see langword="null"/> 时无操作返回。</param>
	/// <remarks>
	/// 同步入口没有 <c>await</c> 可用，只能等——但这一次等待<b>发生在入口</b>，
	/// 而不是判定路径的深处：等待点是可枚举的，不在框架内部。
	/// </remarks>
	internal static void Warm(IPermissionChecker checker)
	{
		if (checker != null)
		{
			AsyncContext.Run(() => checker.EnsureResolvedAsync(CancellationToken.None).AsTask());
		}
	}

	/// <summary>
	/// 同步预热行级数据权限判定所需的授权数据（为同步入口阻塞一次）。
	/// </summary>
	/// <param name="authorizer">行级判定实现；为 <see langword="null"/> 时无操作返回。</param>
	/// <param name="scope">对象所属作用域的服务提供程序（与判定时用的必须是同一个）。</param>
	internal static void Warm(IObjectScopeAuthorizer authorizer, IServiceProvider scope)
	{
		if (authorizer != null)
		{
			AsyncContext.Run(() => authorizer.EnsureResolvedAsync(scope, CancellationToken.None).AsTask());
		}
	}
}
