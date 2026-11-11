using Nerosoft.Euonia.Execution;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 基于执行上下文的 <see cref="ITenantAccessor"/> 默认实现。
/// </summary>
/// <remarks>
/// 租户状态只保存在 <see cref="AmbientExecutionContext"/> 中，本类型不维护第二份存储，
/// 因此工作单元等组件创建的子作用域可以自然继承同一份租户身份。
/// </remarks>
public sealed class TenantAccessor : ITenantAccessor
{
	/// <inheritdoc />
	public TenantContext? Current
	{
		get
		{
			var context = AmbientExecutionContext.Current;
			if (context == null)
			{
				return null;
			}

			var tenant = context.Items.TryGetValue(TenantContextKeys.Info, out var value)
				             ? value as ITenantInfo
				             : null;

			return new TenantContext
			{
				TenantId = context.TenantId,
				ActorTenantId = context.ActorTenantId,
				TargetTenantId = context.TargetTenantId,
				Tenant = tenant,
				IsCrossTenant = context.IsCrossTenant,
				CrossTenantReason = context.CrossTenantReason,
			};
		}
	}

	/// <inheritdoc />
	public string? TenantId => AmbientExecutionContext.Current?.TenantId;
}
