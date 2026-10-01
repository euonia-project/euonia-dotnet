namespace Nerosoft.Euonia.Security;

/// <summary>
/// 明确声明「本应用没有任何方法级权限码」的来源。
/// </summary>
/// <remarks>
/// <para>
/// 选它即等于断言：<b>方法上的 <see cref="PermissionAttribute"/> 不参与判定</b>，因此它是<b>显式选择</b>而非「忘了提供来源」的默认值；
/// <c>AddPermission</c> 要求传入真正的 <see cref="IPermissionCodeSource"/>，缺失即失败。
/// </para>
/// <para>
/// 适用于「只有模型、没有权限码」的应用：行级判定仍按模型声明的策略走，
/// 只是没有任何码会被解析出来。
/// </para>
/// </remarks>
internal sealed class EmptyCodeSource : IPermissionCodeSource
{
	/// <summary>
	/// 共享实例。
	/// </summary>
	internal static EmptyCodeSource Instance { get; } = new();

	/// <inheritdoc />
	/// <remarks>
	/// <b>空集，而不是一份默认词表</b>：本来源没有任何规则，因此它对「本应用用到哪些操作」这个问题的诚实回答就是「不知道」——
	/// 操作集由宿主的对象模型决定（见 <c>IObjectOperationResolver</c>），引擎不替它枚举。
	/// 这里曾经回落到 CRUD 五项，那等于把某个宿主框架的词汇表当成引擎的默认值。
	/// </remarks>
	public IReadOnlyList<string> AllOperations { get; } = [];

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation) => [];
}
