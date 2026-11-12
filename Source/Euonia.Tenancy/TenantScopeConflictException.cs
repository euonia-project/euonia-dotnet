namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 当前异步流已存在不同租户作用域时的冲突异常。
/// </summary>
public sealed class TenantScopeConflictException : InvalidOperationException
{
	/// <summary>
	/// 初始化 <see cref="TenantScopeConflictException"/> 的新实例。
	/// </summary>
	/// <param name="currentTenantId">当前租户。</param>
	/// <param name="targetTenantId">目标租户。</param>
	public TenantScopeConflictException(string? currentTenantId, string targetTenantId)
		: base($"Cannot switch tenant from '{currentTenantId}' to '{targetTenantId}' inside an active tenant scope. Use BeginIsolated or BeginCrossTenant instead.")
	{
		CurrentTenantId = currentTenantId;
		TargetTenantId = targetTenantId;
	}

	/// <summary>获取当前租户。</summary>
	public string? CurrentTenantId { get; }

	/// <summary>获取目标租户。</summary>
	public string TargetTenantId { get; }
}
