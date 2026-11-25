using Microsoft.AspNetCore.Authorization;

namespace Microsoft.AspNetCore.Authorization;

/// <summary>
/// 权限码授权要求，承载“访问当前操作所需的权限码”。
/// </summary>
/// <remarks>
/// 权限码集合在构造时做防御性复制并以只读列表暴露：策略实例会被 <see cref="PermissionPolicyProvider"/>
/// 静态缓存，调用方若能原地改写集合，就会污染此后所有请求的判定。
/// </remarks>
public sealed class PermissionRequirement : IAuthorizationRequirement
{
	/// <summary>
	/// 使用指定的权限码初始化 <see cref="PermissionRequirement"/> 的新实例。
	/// </summary>
	/// <param name="permissions">所需权限码；语义为“任一满足”。空集合表示没有任何码可满足（判定恒不通过）。</param>
	/// <exception cref="ArgumentNullException"><paramref name="permissions"/> 为 <see langword="null"/> 时抛出。</exception>
	/// <exception cref="ArgumentException">存在空白权限码项时抛出（与 <see cref="RequiresPermissionAttribute"/> 口径对齐）。</exception>
	public PermissionRequirement(IEnumerable<string> permissions)
	{
		ArgumentNullException.ThrowIfNull(permissions);

		var codes = permissions.ToArray();

		if (codes.Any(string.IsNullOrWhiteSpace))
		{
			throw new ArgumentException("权限码不能为空白。", nameof(permissions));
		}

		Permissions = Array.AsReadOnly(codes);
	}

	/// <summary>
	/// 获取所需权限码集合（构造时的防御性副本，只读；语义为“任一满足”）。
	/// </summary>
	public IReadOnlyList<string> Permissions { get; }

	/// <inheritdoc />
	public override string ToString()
	{
		return $"Permission({string.Join(" | ", Permissions)})";
	}
}
