using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 基于执行上下文的 <see cref="ITenantScopeFactory"/> 默认实现。
/// </summary>
public sealed class TenantScopeFactory : ITenantScopeFactory
{
	private readonly Microsoft.Extensions.Options.IOptionsMonitor<MultiTenancyOptions>? _options;
	private readonly MultiTenancyOptions? _staticOptions;

	/// <summary>
	/// 初始化 <see cref="TenantScopeFactory"/> 的新实例。
	/// </summary>
	public TenantScopeFactory()
	{
	}

	/// <summary>
	/// 初始化 <see cref="TenantScopeFactory"/> 的新实例。
	/// </summary>
	/// <param name="options">多租户选项，用于跨租户授权。</param>
	public TenantScopeFactory(Microsoft.Extensions.Options.IOptionsMonitor<MultiTenancyOptions> options)
	{
		_options = options;
	}

	/// <summary>
	/// 初始化 <see cref="TenantScopeFactory"/> 的新实例。
	/// </summary>
	/// <param name="options">固定的多租户选项。</param>
	public TenantScopeFactory(MultiTenancyOptions options)
	{
		_staticOptions = options;
	}

	/// <inheritdoc />
	public ITenantScope Begin(ITenantInfo tenant)
	{
		ArgumentNullException.ThrowIfNull(tenant);
		TenantIdValidator.Validate(tenant.Id);
		EnsureCanSwitch(tenant.Id);

		return new Scope(AmbientExecutionContext.Push(context => Apply(context, tenant)));
	}

	/// <inheritdoc />
	public ITenantScope BeginIsolated(ITenantInfo tenant)
	{
		ArgumentNullException.ThrowIfNull(tenant);
		TenantIdValidator.Validate(tenant.Id);

		return new Scope(AmbientExecutionContext.PushNew(context =>
		{
			Apply(context, tenant);
			context.ActorTenantId = tenant.Id;
		}));
	}

	/// <inheritdoc />
	public ITenantScope Begin(ITenantInfo tenant, string? actorTenantId, string? targetTenantId, string? crossTenantReason)
	{
		ArgumentNullException.ThrowIfNull(tenant);

		if (string.IsNullOrEmpty(actorTenantId) || string.IsNullOrEmpty(targetTenantId) || string.IsNullOrWhiteSpace(crossTenantReason))
		{
			throw new ArgumentException("Cross-tenant scope requires actorTenantId, targetTenantId and crossTenantReason together.");
		}

		EnsureCanSwitch(tenant.Id);
		if (!string.Equals(tenant.Id, targetTenantId, StringComparison.Ordinal))
		{
			throw new ArgumentException($"Target tenant '{targetTenantId}' does not match the scope tenant '{tenant.Id}'.");
		}

		TenantIdValidator.Validate(actorTenantId);
		TenantIdValidator.Validate(targetTenantId);

		return new Scope(AmbientExecutionContext.Push(context =>
		{
			Apply(context, tenant);
			context.ActorTenantId = actorTenantId;
			context.TargetTenantId = targetTenantId;

			// 同租户不得被标记为跨租户，否则下游删除/查询会误判为全租户授权。
			context.IsCrossTenant = !string.Equals(actorTenantId, targetTenantId, StringComparison.Ordinal);
			context.CrossTenantReason = context.IsCrossTenant ? crossTenantReason : null;
		}));
	}

	/// <inheritdoc />
	public ITenantScope BeginCrossTenant(string reason, string? targetTenantId = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(reason);
		if (targetTenantId != null)
		{
			TenantIdValidator.Validate(targetTenantId);
		}

		var options = _options?.CurrentValue ?? _staticOptions;
		var authorized = options?.CrossTenantScopeAuthorizer?.Invoke(targetTenantId) == true
		                 || (targetTenantId == null && options?.AllowSystemCrossTenant == true);
		if (!authorized)
		{
			throw new UnauthorizedAccessException("Cross-tenant scope is not authorized by the host policy.");
		}

		return new Scope(AmbientExecutionContext.Push(context =>
		{
			context.TenantId = targetTenantId;
			context.TenantName = null;
			context.TargetTenantId = targetTenantId;
			context.IsCrossTenant = true;
			context.CrossTenantReason = reason;
			context.Items.Remove(TenantContextKeys.Info);
		}));
	}

	private static void Apply(ExecutionContextValues context, ITenantInfo tenant)
	{
		context.TenantId = tenant.Id;
		context.TenantName = tenant.Name;

		// 系统级全租户作用域内进入具体租户时，把 target 收敛到该租户而不是继续保持“全租户”；
		// 这样既保留操作人/原因，又不会让下游按任意租户授权。
		if (context.IsCrossTenant == true && !string.Equals(context.TargetTenantId, tenant.Id, StringComparison.Ordinal))
		{
			context.TargetTenantId = tenant.Id;
		}

		// 操作人租户：首次进入租户作用域时记录，跨租户切换时保留，审计据此区分 Actor/Target。
		context.ActorTenantId ??= tenant.Id;
		context.Items[TenantContextKeys.Info] = tenant;
	}

	private static void EnsureCanSwitch(string tenantId)
	{
		var current = AmbientExecutionContext.Current;
		if (current?.TenantId != null && !string.Equals(current.TenantId, tenantId, StringComparison.Ordinal))
		{
			throw new TenantScopeConflictException(current.TenantId, tenantId);
		}
	}

	private sealed class Scope : ITenantScope
	{
		private readonly IDisposable _lease;
		private bool _disposed;

		public Scope(IDisposable lease)
		{
			_lease = lease;
			Context = new TenantContext
			{
				TenantId = AmbientExecutionContext.Current!.TenantId,
				ActorTenantId = AmbientExecutionContext.Current!.ActorTenantId,
				TargetTenantId = AmbientExecutionContext.Current!.TargetTenantId,
				IsCrossTenant = AmbientExecutionContext.Current!.IsCrossTenant,
				CrossTenantReason = AmbientExecutionContext.Current!.CrossTenantReason,
				Tenant = AmbientExecutionContext.Current!.Items.TryGetValue(TenantContextKeys.Info, out var value)
					         ? value as ITenantInfo
					         : null,
			};
		}

		public TenantContext Context { get; }

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;
			_lease.Dispose();
		}

		public ValueTask DisposeAsync()
		{
			Dispose();
			return ValueTask.CompletedTask;
		}
	}
}
