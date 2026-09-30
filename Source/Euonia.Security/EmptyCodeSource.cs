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
internal sealed class EmptyCodeSource : IPermissionCodeSource
{
	// 注意声明顺序：Instance 的初始化会构造本类实例，实例字段初始化器会读 Operations，
	// 因此 Operations 必须排在 Instance 之前（静态字段按文本顺序初始化）。
	private static readonly string[] Operations = [.. BusinessOperation.All];

	/// <summary>
	/// 共享实例。
	/// </summary>
	internal static EmptyCodeSource Instance { get; } = new();

	/// <inheritdoc />
	/// <remarks>包一层只读：原本直接暴露 <c>string[]</c>，强转后可改写这个被所有宿主共享的静态数组。</remarks>
	public IReadOnlyList<string> AllOperations { get; } = Array.AsReadOnly(Operations);

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation) => [];
}
