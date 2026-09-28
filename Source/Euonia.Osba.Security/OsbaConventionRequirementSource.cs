using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Security;

/// <summary>
/// 把 Osba 的默认要求来源（工厂约定扫描）接到策略引擎的权限码来源上。
/// </summary>
/// <remarks>
/// <b>单向桥</b>：本类只读取 <see cref="ObjectPermissionRequirementProvider.Instance"/>，
/// 绝不反向查询容器里的 <see cref="IPermissionCodeSource"/>——否则运行期会形成
/// 「Osba 问引擎 → 引擎问本类 → 本类又问 Osba」的环。
/// </remarks>
internal sealed class OsbaConventionRequirementSource : IPermissionRequirementSource
{
	internal static OsbaConventionRequirementSource Instance { get; } = new();

	private OsbaConventionRequirementSource()
	{
	}

	/// <inheritdoc />
	public IReadOnlyList<string> AllOperations => BusinessOperation.All;

	/// <inheritdoc />
	public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation)
	{
		return ObjectPermissionRequirementProvider.Instance.RequirementsFor(type, operation);
	}

	/// <inheritdoc />
	public IReadOnlyCollection<string> CodesFor(Type type, string operation)
	{
		return
		[
			.. RequirementsFor(type, operation)
			   .Select(requirement => requirement.Permission)
			   .Where(permission => !string.IsNullOrEmpty(permission))
			   .Distinct(StringComparer.Ordinal)
		];
	}
}
