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
}
