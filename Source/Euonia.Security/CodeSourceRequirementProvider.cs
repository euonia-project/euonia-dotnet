namespace Nerosoft.Euonia.Security;

/// <summary>
/// 把任意权限码来源回答成「要求」：能回答要求的来源原样取用，只给权限码的来源折算成「有码、无角色」。
/// </summary>
/// <remarks>
/// <para>
/// 由 <c>AddPermission</c> 一并注册：<b>引擎自己保证</b>
/// 容器里总有可用的 <see cref="IPermissionRequirementProvider"/>，宿主与宿主框架因此不必各自写一份
/// 形状相同、职责相同的转换代码。
/// </para>
/// <para>
/// 折算而不是丢弃：只给权限码的来源如果被当成「没有要求」，闸门会比来源本身更宽松——那是
/// 静默放行；而角色要求本就不在这类来源的表达力之内，折算后的结果恰好是它所能表达的全部。
/// </para>
/// </remarks>
internal sealed class CodeSourceRequirementProvider(IPermissionCodeSource codeSource) : IPermissionRequirementProvider
{
	/// <inheritdoc />
	public IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation)
	{
		return codeSource is IPermissionRequirementProvider capable
			? capable.RequirementsFor(type, operation)
			: [.. codeSource.CodesFor(type, operation).Select(code => new PermissionAttribute(code))];
	}
}
