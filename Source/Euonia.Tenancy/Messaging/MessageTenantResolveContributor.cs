using Nerosoft.Euonia.Tenancy;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 从可信消息信封解析租户；Order 100，低于用户声明与宿主来源。
/// </summary>
public sealed class MessageTenantResolveContributor : ITenantResolveContributor
{
	/// <inheritdoc />
	public int Order => 100;

	/// <inheritdoc />
	public ValueTask<string?> ResolveAsync(TenantResolveContext context, CancellationToken cancellationToken = default)
	{
		return ValueTask.FromResult(context.IsTrustedTransport ? context.MessageTenantId : null);
	}
}
