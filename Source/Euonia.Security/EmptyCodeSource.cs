namespace Nerosoft.Euonia.Security;

/// <summary>
/// 明确声明「本应用没有任何方法级权限码」的来源。
/// </summary>
/// <remarks>
/// <para>
/// 选它即等于向引擎做出一个断言：<b>方法上的 <see cref="PermissionAttribute"/> 不会参与判定</b>。
/// 因此它是一个<b>显式选择</b>，而不是「忘了提供来源」的默认值——
/// 忘记提供时，<c>AddPermission</c> 要求传入真正的 <see cref="IPermissionCodeSource"/>
/// 并在此失败。
/// </para>
/// <para>
/// 适用场景：应用只使用类型级 <see cref="PermissionAttribute"/> 与模型默认策略。
/// 此时所有操作都只会解析出各自的默认键（<c>@read</c>/<c>@create</c>/…）。
/// </para>
/// <para>
/// <b>不可与按码声明的策略（<c>ScopePolicySet{T}.For(code, …)</c>）同用</b>：
/// 没有任何操作能解析到应用自定义的码，注册期的死策略校验会以
/// 「声明了行级策略，但没有任何操作会解析到该码」拒绝启动。这不是缺陷——
/// 声明了按码策略就说明存在方法级权限码，那就必须给出方法与操作的对应关系。
/// </para>
/// </remarks>
public sealed class EmptyCodeSource : IPermissionCodeSource
{
	/// <summary>
	/// 共享实例。
	/// </summary>
	public static EmptyCodeSource Instance { get; } = new();

	private static readonly BusinessOperation[] Operations = Enum.GetValues<BusinessOperation>();

	/// <inheritdoc />
	public IReadOnlyList<BusinessOperation> AllOperations => Operations;

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, BusinessOperation operation) => [];
}
