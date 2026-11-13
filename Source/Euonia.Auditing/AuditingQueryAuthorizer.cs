using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Auditing;

/// <summary>
/// 审计查询的越权防护：默认只能查询操作人租户，跨租户必须显式声明并给出原因。
/// </summary>
public static class AuditingQueryAuthorizer
{
	private static Func<AuditingQuery, bool>? _crossTenantAuthorization;

	/// <summary>
	/// 配置跨租户查询授权回调；未配置时一律拒绝 <c>AllowCrossTenant</c> 查询。
	/// </summary>
	/// <remarks>仅供宿主启动/AddAuditing 使用；业务代码不应在运行期修改全局授权策略。</remarks>
	/// <param name="authorization">返回 <c>true</c> 表示允许；<c>null</c> 表示拒绝全部跨租户查询。</param>
	[System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
	public static void ConfigureCrossTenantAuthorization(Func<AuditingQuery, bool>? authorization)
	{
		_crossTenantAuthorization = authorization;
	}
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

		// 调用方不能仅靠自设 bool 越过租户隔离：必须由宿主注册显式授权策略。
		if (_crossTenantAuthorization?.Invoke(query) != true)
		{
			throw new UnauthorizedAccessException("Cross-tenant auditing query is not authorized by the host policy.");
		}
	}

	/// <summary>
	/// 校验按作用域读取是否落在操作人租户内；由存储实现调用。
	/// </summary>
	/// <param name="scopeId">作用域标识（格式为 <c>{tenant}:{operation}</c>）。</param>
	/// <exception cref="UnauthorizedAccessException">作用域属于其他租户。</exception>
	public static void EnsureScopeAccess(string scopeId)
	{
		var actorTenantId = AmbientExecutionContext.Current?.ActorTenantId;
		var context = AmbientExecutionContext.Current;
		if (context?.IsCrossTenant == true)
		{
			// 显式跨租户：全租户模式允许任意租户/系统作用域；定向模式只允许目标租户与系统作用域。
			if (context.TargetTenantId == null
			    || scopeId.StartsWith("-:", StringComparison.Ordinal)
			    || scopeId.StartsWith($"{context.TargetTenantId}:", StringComparison.Ordinal))
			{
				return;
			}
		}

		if (string.IsNullOrEmpty(actorTenantId))
		{
			throw new UnauthorizedAccessException("Reading an auditing scope without a tenant context is not allowed.");
		}

		if (!scopeId.StartsWith($"{actorTenantId}:", StringComparison.Ordinal))
		{
			throw new UnauthorizedAccessException($"Tenant '{actorTenantId}' cannot read auditing scope '{scopeId}'.");
		}
	}

	/// <summary>
	/// 获取保留删除应限定的租户；<c>null</c> 表示全租户。
	/// </summary>
	public static string? DeleteTenantScope => AmbientExecutionContext.Current?.TargetTenantId;

	/// <summary>
	/// 校验删除/保留操作是否处于显式跨租户上下文。
	/// </summary>
	/// <exception cref="UnauthorizedAccessException">不在跨租户上下文。</exception>
	public static void EnsureDeleteAuthorized()
	{
		var context = AmbientExecutionContext.Current;
		if (context?.IsCrossTenant != true
		    || string.IsNullOrWhiteSpace(context.CrossTenantReason)
		    || (!string.IsNullOrEmpty(context.ActorTenantId)
		        && !string.IsNullOrEmpty(context.TargetTenantId)
		        && string.Equals(context.ActorTenantId, context.TargetTenantId, StringComparison.Ordinal)))
		{
			throw new UnauthorizedAccessException("Deleting auditing records requires an explicit cross-tenant scope with different actor/target and a reason.");
		}
	}

	/// <summary>
	/// 校验并返回规范化后的查询副本（不修改调用方对象）。
	/// </summary>
	/// <param name="query">查询条件。</param>
	/// <returns>规范化副本。</returns>
	/// <exception cref="UnauthorizedAccessException">请求的租户超出操作人范围。</exception>
	public static AuditingQuery Normalize(AuditingQuery query)
	{
		ArgumentNullException.ThrowIfNull(query);
		Authorize(query);

		var normalized = new AuditingQuery
		{
			// 空白 TenantId 视为未指定，回填操作人租户；禁止空串进入查询层。
			TenantId = string.IsNullOrWhiteSpace(query.TenantId) ? null : query.TenantId,
			UserId = query.UserId,
			CorrelationId = query.CorrelationId,
			IsSuccess = query.IsSuccess,
			From = query.From,
			To = query.To,
			Skip = Math.Max(0, query.Skip),
			Take = query.Take switch
			{
				0 => 50,
				< 0 => throw new ArgumentOutOfRangeException(nameof(query), "Take cannot be negative."),
				_ => Math.Min(query.Take, 500),
			},
			AllowCrossTenant = query.AllowCrossTenant,
			CrossTenantReason = query.CrossTenantReason,
		};

		var actorTenantId = AmbientExecutionContext.Current?.ActorTenantId;
		if (string.IsNullOrEmpty(actorTenantId))
		{
			// 无租户上下文（后台/系统）也必须显式声明跨租户查询，避免匿名/跳过租户解析的请求读到全量数据。
			if (!query.AllowCrossTenant)
			{
				throw new UnauthorizedAccessException("Querying auditing records without a tenant context requires AllowCrossTenant and a reason.");
			}

			return normalized;
		}

		if (query.AllowCrossTenant)
		{
			return normalized;
		}

		if (!string.IsNullOrEmpty(normalized.TenantId) && !string.Equals(normalized.TenantId, actorTenantId, StringComparison.Ordinal))
		{
			throw new UnauthorizedAccessException($"Tenant '{actorTenantId}' cannot query auditing records of tenant '{normalized.TenantId}'.");
		}

		normalized.TenantId ??= actorTenantId;
		return normalized;
	}
}
