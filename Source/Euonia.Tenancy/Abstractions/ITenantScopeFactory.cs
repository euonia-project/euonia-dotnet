namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 定义安装租户作用域的契约。
/// </summary>
public interface ITenantScopeFactory
{
	/// <summary>
	/// 以完整租户信息安装作用域。
	/// </summary>
	/// <param name="tenant">已解析并校验的租户。</param>
	/// <returns>释放时恢复进入前上下文的租户作用域。</returns>
	ITenantScope Begin(ITenantInfo tenant);

	/// <summary>
	/// 以全新快照安装租户作用域，不继承当前流的身份信息。
	/// </summary>
	/// <param name="tenant">已解析并校验的租户。</param>
	/// <returns>释放时恢复进入前上下文的租户作用域。</returns>
	ITenantScope BeginIsolated(ITenantInfo tenant);

	/// <summary>
	/// 进入显式跨租户作用域。
	/// </summary>
	/// <param name="reason">跨租户原因；必填。</param>
	/// <param name="targetTenantId">目标租户；为 <c>null</c> 时是全租户只读模式。</param>
	/// <returns>释放时恢复进入前上下文的租户作用域。</returns>
	ITenantScope BeginCrossTenant(string reason, string? targetTenantId = null);

	/// <summary>
	/// 以消息信封中的操作人/目标语义安装作用域。
	/// </summary>
	/// <remarks>
	/// 当 <paramref name="actorTenantId"/> 与 <paramref name="targetTenantId"/> 不同时属于跨租户访问，
	/// 必须通过宿主配置的跨租户授权策略；未授权时抛出 <see cref="UnauthorizedAccessException"/>。
	/// 两者相同时为同租户访问，无需授权。
	/// </remarks>
	/// <param name="tenant">目标租户。</param>
	/// <param name="actorTenantId">操作人租户；必填。</param>
	/// <param name="targetTenantId">目标租户；必填，且必须等于 <paramref name="tenant"/> 的标识。</param>
	/// <param name="crossTenantReason">跨租户原因；必填；同租户时不会被标记为跨租户。</param>
	/// <returns>租户作用域。</returns>
	/// <exception cref="UnauthorizedAccessException">跨租户访问未获宿主策略授权。</exception>
	ITenantScope Begin(ITenantInfo tenant, string? actorTenantId, string? targetTenantId, string? crossTenantReason);
}
