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
/// 只适用于「仅用类型级 <see cref="PermissionAttribute"/> 与模型默认策略」的应用，此时各操作解析出各自的默认键。
/// </para>
/// <para>
/// <b>不可与按码声明的策略（<c>ScopePolicySet{T}.For(code, …)</c>）同用</b>：没有任何操作能解析到应用自定义的码，死策略校验会拒绝启动（见 README §3.1、DESIGN §1.6）。
/// </para>
/// </remarks>
public sealed class EmptyCodeSource : IPermissionCodeSource
{
	/// <summary>
	/// 共享实例。
	/// </summary>
	public static EmptyCodeSource Instance { get; } = new();

	private static readonly string[] Operations = [.. BusinessOperation.All];

	/// <inheritdoc />
	public IReadOnlyList<string> AllOperations => Operations;

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation) => [];
}
