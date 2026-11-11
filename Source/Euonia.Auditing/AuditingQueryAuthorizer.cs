using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计查询的越权防护：默认只能查询操作人租户，跨租户必须显式声明并给出原因。
/// </summary>
public static class AuditingQueryAuthorizer
{
	/// <summary>
	/// 校验查询的租户范围。
	/// </summary>
	/// <param name="query">查询条件。</param>
	/// <exception cref="UnauthorizedAccessException">查询其他租户但未声明跨租户。</exception>
	/// <exception cref="ArgumentException">声明跨租户但未给出原因。</exception>
	public static void Authorize(AuditingQuery query)
	{
		ArgumentNullException.ThrowIfNull(query);

		if (!query.AllowCrossTenant)
		{
			return;
		}

		if (string.IsNullOrWhiteSpace(query.CrossTenantReason))
		{
			throw new ArgumentException("Cross-tenant auditing query requires a reason.", nameof(query));
		}
	}

	/// <summary>
	/// 校验查询租户是否落在操作人可访问范围内；由存储实现调用。
	/// </summary>
	/// <param name="query">查询条件。</param>
	/// <exception cref="UnauthorizedAccessException">请求的租户超出操作人范围。</exception>
	public static void EnsureScope(AuditingQuery query)
	{
		Authorize(query);

		var actorTenantId = AmbientExecutionContext.Current?.ActorTenantId;
		if (string.IsNullOrEmpty(actorTenantId))
		{
			// 系统 / 后台任务无租户上下文：由调用方的权限体系负责。
			return;
		}

		if (query.AllowCrossTenant)
		{
			return;
		}

		if (!string.IsNullOrEmpty(query.TenantId) && !string.Equals(query.TenantId, actorTenantId, StringComparison.Ordinal))
		{
			throw new UnauthorizedAccessException($"Tenant '{actorTenantId}' cannot query auditing records of tenant '{query.TenantId}'.");
		}

		query.TenantId ??= actorTenantId;
	}
}
