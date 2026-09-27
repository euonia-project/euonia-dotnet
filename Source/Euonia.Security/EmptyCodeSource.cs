namespace Nerosoft.Euonia.Security;

/// <summary>
/// 不声明任何权限码的来源，用于空注册表与「未接对象模型」的独立使用场景。
/// </summary>
/// <remarks>
/// 此时所有操作都只会解析出各自的默认键，权限码级策略与死策略校验自然无从谈起——
/// 但这不是<b>静默放宽</b>：没有方法级 <see cref="PermissionAttribute"/> 可扫，
/// 也就没有「操作权限码」可言，写侧仍由调用方显式传入。
/// </remarks>
internal sealed class EmptyCodeSource : IPermissionCodeSource
{
	internal static EmptyCodeSource Instance { get; } = new();

	private static readonly BusinessOperation[] Operations = Enum.GetValues<BusinessOperation>();

	public IReadOnlyList<BusinessOperation> AllOperations => Operations;

	public IReadOnlyCollection<string> CodesFor(Type type, BusinessOperation operation) => [];
}
