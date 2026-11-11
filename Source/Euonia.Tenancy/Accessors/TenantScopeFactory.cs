using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 基于执行上下文的 <see cref="ITenantScopeFactory"/> 默认实现。
/// </summary>
public sealed class TenantScopeFactory : ITenantScopeFactory
{
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
	public ITenantScope BeginCrossTenant(string reason, string? targetTenantId = null)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(reason);
		if (targetTenantId != null)
		{
			TenantIdValidator.Validate(targetTenantId);
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
		context.TargetTenantId = null;
		context.IsCrossTenant = false;
		context.CrossTenantReason = null;

		// 操作人租户：首次进入租户作用域时记录，跨租户切换时保留，审计据此区分 Actor/Target。
		context.ActorTenantId ??= tenant.Id;
		context.Items[TenantContextKeys.Info] = tenant;
	}

	private static void EnsureCanSwitch(string tenantId)
	{
		var current = AmbientExecutionContext.Current;
		if (current?.TenantId != null && !string.Equals(current.TenantId, tenantId, StringComparison.Ordinal))
		{
			throw new InvalidOperationException($"Cannot switch tenant from '{current.TenantId}' to '{tenantId}' inside an active tenant scope. Use BeginIsolated or BeginCrossTenant instead.");
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
