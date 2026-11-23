namespace Nerosoft.Euonia.Security;

/// <summary>
/// 声明执行业务操作所需的权限要求，可应用于资源类型或执行该操作的方法。
/// </summary>
/// <remarks>
/// <para>
/// <b>本类型位于 <c>Euonia.Core</c> 程序集</b>（命名空间沿用 <c>Nerosoft.Euonia.Security</c>，与
/// <c>UserPrincipal</c>、<c>UserClaimTypes</c> 同类）：权限的「基础词汇」必须能被不引入策略引擎的宿主使用——
/// 业务对象上的声明、对象工厂边界的判定，都不应要求宿主安装引擎。
/// </para>
/// <para>
/// <see cref="Permission"/> 是<b>任意字符串、单一维度</b>：可含 <c>:</c>、<c>-</c>、<c>.</c> 等字符，
/// 不得以框架保留前缀 <c>@</c> 开头，首尾空白会被裁剪。「<c>资源:动作</c>」只是命名习惯，
/// 码本身没有分段语义，也不需要任何前缀或格式约定。
/// </para>
/// <para>
/// 类型级要求对该类型支持的全部操作生效；方法级要求<b>只在该方法被执行时生效</b>，不波及同类型的其它操作
/// （两者取并集）。未指定 <see cref="Permission"/> 时仅校验角色，未指定角色时仅校验权限；
/// 两者均未指定表示「无要求」。
/// </para>
/// </remarks>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public sealed class PermissionAttribute : Attribute
{
	/// <summary>
	/// 使用指定的权限码初始化 <see cref="PermissionAttribute"/> 的新实例。
	/// </summary>
	/// <param name="permission">执行操作所需的权限码，支持以 <c>*</c> 结尾的前缀通配符匹配。</param>
	public PermissionAttribute(string permission)
		: this(permission, Array.Empty<string>())
	{
	}

	/// <summary>
	/// 使用指定的权限码和角色初始化 <see cref="PermissionAttribute"/> 的新实例。
	/// </summary>
	/// <param name="permission">执行操作所需的权限码，支持以 <c>*</c> 结尾的前缀通配符匹配。可以为 <see langword="null"/> 或空字符串。</param>
	/// <param name="roles">允许执行操作的角色名称数组，满足任意一个角色即可。</param>
	public PermissionAttribute(string permission, params string[] roles)
	{
		Permission = permission;
		Roles = roles ?? Array.Empty<string>();
	}

	/// <summary>
	/// 获取执行操作所需的权限码（任意非空字符串；只想校验角色时为空；首尾空白在读取时被裁剪）。
	/// </summary>
	/// <remarks>该值是只读的：属性没有 setter，权限码在实例构造时确定，调用方不应也无法原地修改。</remarks>
	public string Permission { get; }

	/// <summary>
	/// 获取允许执行操作的角色名称数组。
	/// </summary>
	/// <remarks>返回的数组是声明实例的一部分，调用方不应原地修改；需要变更时请构造新的 <see cref="PermissionAttribute"/>。</remarks>
	public string[] Roles { get; }

	/// <summary>
	/// 获取或设置权限被拒绝时的提示消息。框架自身不读取它，由使用方在拒绝路径上取用。
	/// </summary>
	public string Message { get; set; } = string.Empty;
}
