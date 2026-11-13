namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 租户写入规则；纯状态校验，不依赖 EF Core。
/// </summary>
public static class TenantEntryGuard
{
	/// <summary>
	/// 校验写入并返回新增实体应写入的租户标识。
	/// </summary>
	/// <param name="context">写入状态。</param>
	/// <returns>新增实体需要写入的租户标识；无需赋值时为 <c>null</c>。</returns>
	/// <exception cref="CrossTenantOperationException">跨租户写入或租户迁移被拒绝时抛出。</exception>
	/// <exception cref="TenantException">缺少租户或租户状态不允许写入时抛出。</exception>
	public static string? Validate(in TenantEntryContext context)
	{
		if (context.Operation == TenantEntryOperation.None)
		{
			return null;
		}

		if (!string.IsNullOrEmpty(context.OriginalTenantId)
		    && !string.IsNullOrEmpty(context.EntityTenantId)
		    && !string.Equals(context.OriginalTenantId, context.EntityTenantId, StringComparison.Ordinal))
		{
			throw new CrossTenantOperationException("The tenant of an existing entity cannot be changed.");
		}

		if (context.ScopeTenantStatus != TenantStatus.Active && !context.AllowInactiveWrites)
		{
			throw new TenantSuspendedException(context.ScopeTenantId ?? context.TargetTenantId ?? "-", context.ScopeTenantStatus);
		}

		if (context.IsCrossTenant)
		{
			if (string.IsNullOrEmpty(context.ScopeTenantId))
			{
				throw new CrossTenantOperationException("The all-tenant cross-tenant scope is read-only; use a target tenant scope for writes.");
			}

			return context.Operation == TenantEntryOperation.Added
				       ? ResolveCrossTenantAdded(context)
				       : ValidateExisting(context, context.ScopeTenantId);
		}

		if (string.IsNullOrEmpty(context.ScopeTenantId))
		{
			throw new TenantRequiredException();
		}

		return context.Operation == TenantEntryOperation.Added
			       ? ResolveAdded(context, context.ScopeTenantId)
			       : ValidateExisting(context, context.ScopeTenantId);
	}

	private static string? ResolveCrossTenantAdded(in TenantEntryContext context)
	{
		return ResolveAdded(context, context.ScopeTenantId!);
	}

	private static string? ResolveAdded(in TenantEntryContext context, string scopeTenantId)
	{
		if (string.IsNullOrEmpty(context.EntityTenantId))
		{
			return scopeTenantId;
		}

		if (!string.Equals(context.EntityTenantId, scopeTenantId, StringComparison.Ordinal))
		{
			throw new CrossTenantOperationException($"Cannot add an entity of tenant '{context.EntityTenantId}' inside tenant scope '{scopeTenantId}'.");
		}

		return null;
	}

	private static string? ValidateExisting(in TenantEntryContext context, string scopeTenantId)
	{
		if (!string.Equals(context.OriginalTenantId, scopeTenantId, StringComparison.Ordinal)
		    || !string.Equals(context.EntityTenantId, scopeTenantId, StringComparison.Ordinal))
		{
			throw new CrossTenantOperationException($"Cannot modify or delete tenant data outside the current tenant scope '{scopeTenantId}'.");
		}

		return null;
	}
}
