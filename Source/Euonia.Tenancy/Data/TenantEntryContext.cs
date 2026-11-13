namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户写入守卫的纯状态输入；持久化适配层负责把实体条目映射为该结构。
/// </summary>
public readonly struct TenantEntryContext
{
	/// <summary>
	/// 初始化 <see cref="TenantEntryContext"/> 的新实例。
	/// </summary>
	/// <param name="operation">写入操作。</param>
	/// <param name="entityTenantId">实体当前租户标识。</param>
	/// <param name="originalTenantId">修改/删除前的原始租户标识。</param>
	/// <param name="scopeTenantId">当前数据作用域租户标识。</param>
	/// <param name="targetTenantId">跨租户目标租户标识。</param>
	/// <param name="scopeTenantStatus">当前租户状态；无租户信息时为 <see cref="TenantStatus.Active"/>。</param>
	/// <param name="isCrossTenant">是否处于显式跨租户作用域。</param>
	/// <param name="allowInactiveWrites">是否允许写入停用/归档租户。</param>
	public TenantEntryContext(
		TenantEntryOperation operation,
		string? entityTenantId,
		string? originalTenantId,
		string? scopeTenantId,
		string? targetTenantId,
		TenantStatus scopeTenantStatus,
		bool isCrossTenant,
		bool allowInactiveWrites)
	{
		Operation = operation;
		EntityTenantId = entityTenantId;
		OriginalTenantId = originalTenantId;
		ScopeTenantId = scopeTenantId;
		TargetTenantId = targetTenantId;
		ScopeTenantStatus = scopeTenantStatus;
		IsCrossTenant = isCrossTenant;
		AllowInactiveWrites = allowInactiveWrites;
	}

	/// <summary>
	/// 获取写入操作。
	/// </summary>
	public TenantEntryOperation Operation { get; }

	/// <summary>
	/// 获取实体当前租户标识。
	/// </summary>
	public string? EntityTenantId { get; }

	/// <summary>
	/// 获取修改/删除前的原始租户标识。
	/// </summary>
	public string? OriginalTenantId { get; }

	/// <summary>
	/// 获取当前数据作用域租户标识。
	/// </summary>
	public string? ScopeTenantId { get; }

	/// <summary>
	/// 获取跨租户目标租户标识。
	/// </summary>
	public string? TargetTenantId { get; }

	/// <summary>
	/// 获取当前租户状态。
	/// </summary>
	public TenantStatus ScopeTenantStatus { get; }

	/// <summary>
	/// 获取一个值，指示是否处于显式跨租户作用域。
	/// </summary>
	public bool IsCrossTenant { get; }

	/// <summary>
	/// 获取一个值，指示是否允许写入停用/归档租户。
	/// </summary>
	public bool AllowInactiveWrites { get; }
}
