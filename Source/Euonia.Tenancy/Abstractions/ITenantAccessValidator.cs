using System.Security.Claims;

namespace Nerosoft.Euonia.Tenancy;

/// <summary>
/// 定义“当前用户是否有权访问指定租户”的校验契约。
/// </summary>
public interface ITenantAccessValidator
{
	/// <summary>
	/// 判断用户是否有权访问指定租户。
	/// </summary>
	/// <param name="user">当前用户；匿名时为 <c>null</c>。</param>
	/// <param name="tenant">已解析的租户。</param>
	/// <param name="cancellationToken">取消令牌。</param>
	/// <returns>有权访问时为 <see langword="true"/>。</returns>
	ValueTask<bool> CanAccessAsync(ClaimsPrincipal? user, ITenantInfo tenant, CancellationToken cancellationToken = default);
}
