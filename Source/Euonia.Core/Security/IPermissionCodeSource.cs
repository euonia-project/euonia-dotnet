namespace Nerosoft.Euonia.Security;

/// <summary>
/// 兼容入口：按「某类型在某操作上要求什么」回答权限码与权限要求。
/// </summary>
/// <remarks>
/// <para>
/// 按 <c>(类型, 操作)</c> 回答，不包含「被调用的方法」这一坐标。需要按方法回答时使用
/// <see cref="IPermissionRequirementSource"/>；直接读取类型与方法上的
/// <see cref="PermissionAttribute"/> 时使用 <see cref="PermissionRequirements"/>。
/// </para>
/// <para>
/// 实现者通常只需实现 <see cref="AllOperations"/> 与 <see cref="CodesFor"/>：
/// <see cref="RequirementsFor"/> 有默认实现，把权限码折算为「有码、无角色」的要求。
/// 能表达<b>角色</b>要求的实现可以覆写它。
/// </para>
/// </remarks>
[Obsolete("按 (type, operation) 查询的兼容接口。请使用 PermissionRequirements / IPermissionRequirementSource；本接口将在下个主版本移除。")]
public interface IPermissionCodeSource
{
	/// <summary>
	/// 获取本应用使用的全部业务操作。
	/// </summary>
	/// <remarks>
	/// 本应用声明的操作集合，供按操作查询的调用方遍历。权限要求的发现读取类型与方法上的声明，不依赖它。
	/// </remarks>
	IReadOnlyList<string> AllOperations { get; }

	/// <summary>
	/// 获取指定类型在指定操作上声明的全部权限码（含类型级与方法级，忽略空权限名）。
	/// </summary>
	/// <param name="type">资源类型。</param>
	/// <param name="operation">业务操作。</param>
	/// <returns>权限码集合。</returns>
	IReadOnlyCollection<string> CodesFor(Type type, string operation);

	/// <summary>
	/// 收集指定类型在指定操作上声明的全部权限要求（默认把 <see cref="CodesFor"/> 的结果折算为「有码、无角色」）。
	/// </summary>
	/// <param name="type">资源类型。</param>
	/// <param name="operation">业务操作。</param>
	/// <returns>权限要求数组。</returns>
	/// <remarks>
	/// 默认实现的存在是有意的：只给权限码的来源如果被当成「没有要求」，闸门会比来源本身更宽松（静默放行），
	/// 而角色要求本就不在这类来源的表达力之内——折算后的结果恰好是它所能表达的全部。
	/// </remarks>
	IReadOnlyList<PermissionAttribute> RequirementsFor(Type type, string operation)
	{
		return [.. CodesFor(type, operation).Select(code => new PermissionAttribute(code))];
	}
}
