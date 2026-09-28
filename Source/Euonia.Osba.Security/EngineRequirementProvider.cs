using Nerosoft.Euonia.Security;

namespace Nerosoft.Euonia.Osba.Security;

/// <summary>
/// 运行期的要求来源：把容器里注册的权限码来源（Osba 的工厂约定 <b>加上</b>宿主补充的规则）回答给工厂边界。
/// </summary>
/// <remarks>
/// <para>
/// 这一条边是单向的：本类从不被引擎侧的来源反向调用，因此「Osba 问引擎、引擎再问 Osba」不会成环——
/// 引擎侧的约定来源直接读 <see cref="ObjectPermissionRequirementProvider.Instance"/>（静态、不查容器）。
/// </para>
/// <para>
/// 运行期与注册期校验问的是同一个来源（见 Euonia.Security 的 DESIGN §1.11）：
/// 宿主通过 <c>AddPermission</c> 补充的规则在工厂边界同样生效。
/// </para>
/// </remarks>
internal sealed class EngineRequirementProvider(IPermissionCodeSource codeSource) : IPermissionRequirementProvider
{
	/// <inheritdoc />
	public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation)
	{
		// 能回答「要求」的来源直接问它（角色等信息原样保留）；只给权限码的来源折算成「有码、无角色」。
		return codeSource is IPermissionRequirementSource capable
			? capable.RequirementsFor(type, operation)
			: [.. codeSource.CodesFor(type, operation).Select(code => new PermissionAttribute(code))];
	}
}
