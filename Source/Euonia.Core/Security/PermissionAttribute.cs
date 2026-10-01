namespace Nerosoft.Euonia.Security;

/// <summary>
/// 声明执行业务操作所需的权限要求，可应用于资源类型或执行该操作的方法。
/// </summary>
/// <remarks>
/// <b>本类型位于 <c>Euonia.Core</c> 程序集</b>（命名空间沿用 <c>Nerosoft.Euonia.Security</c>，与
/// <c>UserPrincipal</c>、<c>UserClaimTypes</c> 同类）：权限的「基础词汇」必须能被不引入策略引擎的宿主使用——
/// 业务对象上的声明、对象工厂边界的判定，都不应要求宿主安装引擎。
/// </remarks>
/// <remarks>
/// 类型级要求适用于该类型支持的全部操作；方法级要求仅在该方法被执行时生效（两者取并集）。
/// 未指定 <see cref="Permission"/> 时仅校验角色，未指定角色时仅校验权限；两者均未指定则视为放行。
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PermissionAttribute : Attribute
{
	/// <summary>
	/// 使用指定的权限名称初始化 <see cref="PermissionAttribute"/> 的新实例。
	/// </summary>
	/// <param name="permission">执行操作所需的权限名称，支持以 <c>*</c> 结尾的前缀通配符匹配。</param>
	public PermissionAttribute(string permission)
		: this(permission, Array.Empty<string>())
	{
	}

	/// <summary>
	/// 使用指定的权限名称和角色初始化 <see cref="PermissionAttribute"/> 的新实例。
	/// </summary>
	/// <param name="permission">执行操作所需的权限名称，支持以 <c>*</c> 结尾的前缀通配符匹配。可以为 <see langword="null"/> 或空字符串。</param>
	/// <param name="roles">允许执行操作的角色名称数组，满足任意一个角色即可。</param>
	public PermissionAttribute(string permission, params string[] roles)
	{
		Permission = permission;
		Roles = roles ?? Array.Empty<string>();
	}

	/// <summary>
	/// 获取执行操作所需的权限名称。
	/// </summary>
	public string Permission { get; }

	/// <summary>
	/// 获取允许执行操作的角色名称数组。
	/// </summary>
	public string[] Roles { get; }

	/// <summary>
	/// 获取或设置权限被拒绝时的提示消息。框架自身不读取它，由使用方在拒绝路径上取用。
	/// </summary>
	public string Message { get; set; } = string.Empty;
}