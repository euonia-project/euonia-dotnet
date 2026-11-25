using Microsoft.AspNetCore.Authorization;
using Nerosoft.Euonia.Security;

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
	/// <param name="permissions">访问所需的权限码；至少一个，多个为“任一满足”。首尾空白会被裁剪。</param>
	/// <exception cref="ArgumentException">未提供权限码、存在空白权限码，或权限码含策略分隔符 <see cref="PolicySeparator"/> 时抛出。</exception>
	/// <exception cref="InvalidOperationException">权限码使用框架保留前缀 <c>@</c> 时抛出（与启动期声明扫描同一口径）。</exception>
	/// <remarks>
	/// 每个码都经 <see cref="ScopeKeys.Validate"/> 校验：含 <c>|</c> 的码会被 <see cref="PermissionPolicyProvider"/>
	/// 按分隔符拆成“持有任一码即通过”，静默放宽判定，因此构造时显式拒绝。
	/// </remarks>
	public RequiresPermissionAttribute(params string[] permissions)
	{
		if (permissions is not { Length: > 0 } || permissions.Any(string.IsNullOrWhiteSpace))
		{
			throw new ArgumentException("至少需要提供一个权限码。", nameof(permissions));
		}

		var codes = new string[permissions.Length];

		for (var index = 0; index < permissions.Length; index++)
		{
			var code = permissions[index].Trim();

			// '|' 是策略名分隔符：码里含它会被还原成多个码的“或”判定，必须拒绝
			if (code.Contains(PolicySeparator, StringComparison.Ordinal))
			{
				throw new ArgumentException($"权限码 '{permissions[index]}' 含策略分隔符 '{PolicySeparator}'，单个权限码不得包含该字符。", nameof(permissions));
			}

			// 与启动期声明扫描共用唯一校验器：空白与保留前缀 '@' 两入口同一套规则与文案
			ScopeKeys.Validate(code);

			codes[index] = code;
		}

		Permissions = codes;
		Policy = PolicyPrefix + string.Join(PolicySeparator, codes);
	}

	/// <summary>
	/// 获取访问所需的权限码集合（已裁剪首尾空白）。
	/// </summary>
	public string[] Permissions { get; }
}
