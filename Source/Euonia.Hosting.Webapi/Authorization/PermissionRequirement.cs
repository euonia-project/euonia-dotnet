using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// 权限码授权要求，承载“访问当前操作所需的权限码”。
/// </summary>
/// <param name="permissions">所需权限码；语义为“任一满足”。</param>
public sealed class PermissionRequirement(string[] permissions) : IAuthorizationRequirement
{
	/// <summary>
	/// 获取所需权限码集合。
	/// </summary>
	public string[] Permissions { get; } = permissions ?? [];

	/// <inheritdoc />
	public override string ToString()
	{
		return $"Permission({string.Join(" | ", Permissions)})";
	}
}
