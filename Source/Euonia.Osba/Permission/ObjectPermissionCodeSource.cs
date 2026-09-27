using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba;

/// <summary>
/// 把 <see cref="PermissionRequirements"/> 的扫描结果包装成 <see cref="IPermissionCodeSource"/>。
/// </summary>
/// <remarks>
/// 这是「对象模型 → 权限」这条依赖方向的唯一落点：<see cref="PermissionRequirements"/> 需要
/// <c>ObjectReflector</c> 与 <c>Factory*Attribute</c> 才能判定方法角色，因此必须留在 <c>Euonia.Osba</c>；
/// 权限库通过本包装反向取用，而不是自己认识工厂方法。
/// </remarks>
internal sealed class ObjectPermissionCodeSource : IPermissionCodeSource
{
	internal static ObjectPermissionCodeSource Instance { get; } = new();

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, BusinessOperation operation)
		=> PermissionRequirements.CodesFor(type, operation);

	/// <inheritdoc />
	public IReadOnlyList<BusinessOperation> AllOperations => PermissionRequirements.AllOperations;
}
