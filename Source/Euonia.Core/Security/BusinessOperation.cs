namespace Nerosoft.Euonia.Security;

/// <summary>
/// 业务操作的默认词汇表。
/// </summary>
/// <remarks>
/// <para>
/// 操作是<b>字符串</b>而不是枚举：操作集是使用方的领域知识，框架不替它划定边界，宿主可定义自己的操作（<c>"approve"</c>、
/// <c>"publish"</c>…）；操作全集由使用方的权限实现声明（策略引擎侧见 <c>IPermissionCodeSource.AllOperations</c>）。
/// </para>
/// <para>
/// 本类只提供一组<b>约定俗成</b>的操作名（CRUD 与命令执行），不构成限制——它们只是常量字符串。
/// </para>
/// <para>
/// 操作名不得以 <c>@</c> 开头：该前缀是策略键的保留命名空间（由引擎以 <c>@&lt;operation&gt;</c> 派生默认键，
/// 见 Euonia.Security 的 DESIGN §1.6）。
/// </para>
/// </remarks>
public static class BusinessOperation
{
	/// <summary>读取操作。</summary>
	public const string Read = "read";

	/// <summary>创建（插入）操作。</summary>
	public const string Create = "create";

	/// <summary>更新操作。</summary>
	public const string Update = "update";

	/// <summary>删除操作。</summary>
	public const string Delete = "delete";

	/// <summary>命令执行操作。</summary>
	public const string Execute = "execute";

	/// <summary>
	/// 默认词汇表的全部操作。
	/// </summary>
	/// <remarks>
	/// 宿主自定义操作集时不要复用本列表——它只是「没有任何自定义操作时」的合理默认，
	/// 操作全集应由使用方的权限实现声明（策略引擎侧即其权限码来源的 <c>AllOperations</c>）。
	/// </remarks>
	public static IReadOnlyList<string> All { get; } =
	[
		Read,
		Create,
		Update,
		Delete,
		Execute
	];
}
