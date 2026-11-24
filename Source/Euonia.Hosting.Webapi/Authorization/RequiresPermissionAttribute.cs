using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// 声明访问当前操作所需的权限码。
/// </summary>
/// <remarks>
/// 权限码编码到 <see cref="AuthorizeAttribute.Policy"/> 中（形如 <c>Permission:account.user.create</c>），
/// 由 <see cref="PermissionPolicyProvider"/> 还原为 <see cref="PermissionRequirement"/>。
/// 多个权限码为“任一满足”，类与方法同时标注时为“同时满足”。
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class RequiresPermissionAttribute : AuthorizeAttribute
{
	/// <summary>
	/// 权限策略名前缀。
	/// </summary>
	public const string PolicyPrefix = "Permission:";

	/// <summary>
	/// 权限码之间的分隔符。
	/// </summary>
	public const string PolicySeparator = "|";

	/// <summary>
	/// 使用指定的权限码初始化 <see cref="RequiresPermissionAttribute"/>。
	/// </summary>
	/// <param name="permissions">访问所需的权限码；至少一个，多个为“任一满足”。</param>
	/// <exception cref="ArgumentException">未提供权限码或存在空白权限码时抛出。</exception>
	public RequiresPermissionAttribute(params string[] permissions)
	{
		if (permissions is not { Length: > 0 } || permissions.Any(string.IsNullOrWhiteSpace))
		{
			throw new ArgumentException("至少需要提供一个权限码。", nameof(permissions));
		}

		Permissions = [.. permissions];
		Policy = PolicyPrefix + string.Join(PolicySeparator, permissions);
	}

	/// <summary>
	/// 获取访问所需的权限码集合。
	/// </summary>
	public string[] Permissions { get; }
}
