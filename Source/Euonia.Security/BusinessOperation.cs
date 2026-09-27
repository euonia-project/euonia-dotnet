namespace Nerosoft.Euonia.Security;

/// <summary>
/// 业务操作的默认词汇表。
/// </summary>
/// <remarks>
/// <para>
/// 操作是<b>字符串</b>而不是枚举：操作集是使用方的领域知识，框架不替它划定边界。
/// 任何宿主都可以定义自己的操作（<c>"approve"</c>、<c>"publish"</c>…），
/// 由 <see cref="IPermissionCodeSource.AllOperations"/> 声明全集，
/// 由 <see cref="ScopePolicySet{T}.For(string, ScopePolicy{T})"/> 声明策略。
/// </para>
/// <para>
/// 本类提供一组<b>约定俗成</b>的操作名（CRUD 与命令执行），覆盖绝大多数应用，
/// 但不构成限制——它们只是常量字符串。
/// </para>
/// <para>
/// 操作名不得使用 <see cref="ScopeKeys.Prefix"/>（<c>@</c>）前缀：
/// 每个操作的默认策略键由 <see cref="ScopeKeys.For(string)"/> 以该前缀派生，
/// 带前缀会与保留命名空间冲突。
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
	/// 见 <see cref="IPermissionCodeSource.AllOperations"/>。
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
